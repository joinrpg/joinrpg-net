using JoinRpg.Common.WebInfrastructure.Cache;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Users;

namespace Joinrpg.Web.Identity;

public record AvatarCacheKey(AvatarIdentification Id, int Size) { }

/// <summary>
/// Idea of this class to save trip to database to load URI of avatar.
/// This cache cannot be too large (it just id to string), so we never purge it.
/// Avatars are immutable.
/// </summary>
/// <remarks>
/// Одиночный <see cref="IAvatarLoader.GetAvatar"/> — частный случай пакетного
/// <see cref="IAvatarLoader.GetAvatars"/>, кеш у них общий.
/// В кеше лежат задачи, а не готовые значения: промах сначала занимает ключ своей незавершённой
/// задачей (<see cref="SingletonCache{TKey, TValue}.GetOrAdd"/> атомарен), и только потом идёт в БД.
/// Поэтому параллельный запрос той же аватарки не грузит её второй раз, а ждёт уже начатую загрузку.
/// Упавшая задача (аватарки нет в БД или сбой) в кеше не задерживается: следующий запрос
/// атомарно заменяет её своей (<see cref="SingletonCache{TKey, TValue}.TryUpdate"/>) и грузит заново.
/// Каждая занятая задача завершается в любом случае — иначе зависла бы в вечном кеше навсегда.
/// </remarks>
public class AvatarCachedLoader(
    IUserRepository userRepository,
    SingletonCache<AvatarCacheKey, Task<AvatarInfo>> cache,
    IAvatarUriResolver avatarUriResolver) : IAvatarLoader
{
    private const string AvatarTypeName = "userAvatar";

    public async Task<AvatarInfo> GetAvatar(AvatarIdentification userAvatarId, int recommendedSize)
    {
        var avatars = await GetAvatars([userAvatarId], recommendedSize);
        return avatars.TryGetValue(userAvatarId, out var avatar)
            ? avatar
            : throw new JoinRpgEntityNotFoundException(userAvatarId.Value, AvatarTypeName);
    }

    public async Task<IReadOnlyDictionary<AvatarIdentification, AvatarInfo>> GetAvatars(
        IReadOnlyCollection<AvatarIdentification> avatarIds,
        int recommendedSize)
    {
        var tasks = new Dictionary<AvatarIdentification, Task<AvatarInfo>>();
        var toLoad = new Dictionary<AvatarIdentification, TaskCompletionSource<AvatarInfo>>();

        foreach (var avatarId in avatarIds.Distinct())
        {
            var key = new AvatarCacheKey(avatarId, recommendedSize);
            var pending = new TaskCompletionSource<AvatarInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
            var task = ClaimOrGet(key, pending.Task);
            if (task == pending.Task)
            {
                toLoad.Add(avatarId, pending);
            }
            tasks.Add(avatarId, task);
        }

        if (toLoad.Count > 0)
        {
            await LoadMissing(toLoad, recommendedSize);
        }

        var result = new Dictionary<AvatarIdentification, AvatarInfo>(tasks.Count);
        foreach (var (avatarId, task) in tasks)
        {
            try
            {
                result.Add(avatarId, await task);
            }
            catch (JoinRpgEntityNotFoundException)
            {
                // Аватарки нет в БД — в ответ не попадает.
            }
        }
        return result;
    }

    /// <summary>
    /// Возвращает задачу из кеша, а если её нет или она упала — занимает ключ задачей <paramref name="pending"/>.
    /// </summary>
    private Task<AvatarInfo> ClaimOrGet(AvatarCacheKey key, Task<AvatarInfo> pending)
    {
        while (true)
        {
            var cached = cache.GetOrAdd(key, _ => pending);
            if (cached == pending || !cached.IsFaulted)
            {
                return cached;
            }
            // Запомненный сбой — грузим заново. Если кто-то успел заменить его раньше нас, берём его задачу.
            if (cache.TryUpdate(key, pending, cached))
            {
                return pending;
            }
        }
    }

    private async Task LoadMissing(
        Dictionary<AvatarIdentification, TaskCompletionSource<AvatarInfo>> toLoad,
        int recommendedSize)
    {
        try
        {
            var loaded = await userRepository.LoadAvatars(toLoad.Keys);
            var byId = loaded.ToDictionary(a => new AvatarIdentification(a.UserAvatarId));
            foreach (var (avatarId, pending) in toLoad)
            {
                if (!byId.TryGetValue(avatarId, out var avatar))
                {
                    _ = pending.TrySetException(new JoinRpgEntityNotFoundException(avatarId.Value, AvatarTypeName));
                    continue;
                }

                try
                {
                    _ = pending.TrySetResult(
                        new AvatarInfo(avatarUriResolver.GetAvatarUri(avatar.CachedUri, avatar.OriginalUri), recommendedSize));
                }
                catch (Exception exception)
                {
                    // Кривой адрес одной аватарки не должен оставить остальные незавершёнными.
                    _ = pending.TrySetException(exception);
                }
            }
        }
        catch (Exception exception)
        {
            foreach (var pending in toLoad.Values)
            {
                if (pending.TrySetException(exception))
                {
                    // Исключение уходит вызывающему через throw ниже; задачу помечаем наблюдённой,
                    // чтобы при сборке мусора не было UnobservedTaskException.
                    _ = pending.Task.Exception;
                }
            }
            throw;
        }
    }
}
