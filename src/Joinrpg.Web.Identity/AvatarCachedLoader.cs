using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;

namespace Joinrpg.Web.Identity;

public record AvatarCacheKey(AvatarIdentification Id, int Size) { }

/// <summary>
/// Idea of this class to save trip to database to load URI of avatar.
/// This cache cannot be too large (it just id to string), so we never purge it.
/// Avatars are immutable.
/// </summary>
public class AvatarCachedLoader(IUserRepository userRepository, SingletonCache<AvatarCacheKey, Task<AvatarInfo>> cache) : IAvatarLoader
{
    Task<AvatarInfo> IAvatarLoader.GetAvatar(AvatarIdentification userAvatarId, int recommendedSize)
    {
        var key = new AvatarCacheKey(userAvatarId, recommendedSize);
        var cachedTask = cache.GetOrAdd(key, k => Load(k));

        if (cachedTask.IsFaulted)
        {
            //cachedtask is memoized failure. Let's restart (once).
            var restart = Load(key);
            cache.Set(key, restart);
            return restart;
        }
        else
        {
            return cachedTask;
        }

        async Task<AvatarInfo> Load(AvatarCacheKey key)
        {
            var avatar = await userRepository.LoadAvatar(key.Id);
            return new AvatarInfo(new Uri(avatar.CachedUri ?? avatar.OriginalUri), recommendedSize);
        }
    }

}
