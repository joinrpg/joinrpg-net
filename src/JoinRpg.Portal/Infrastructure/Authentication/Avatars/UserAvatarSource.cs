using Joinrpg.Web.Identity;
using JoinRpg.Services.Interfaces.Avatars;
using JoinRpg.Web.UserProfile;

namespace JoinRpg.Portal.Infrastructure.Authentication.Avatars;

/// <remarks>
/// Один экземпляр на запрос, вызовы — строго по очереди. Соседние компоненты в одном дереве рендера
/// запускают OnParametersSetAsync, не дожидаясь друг друга, а EF6-контекст запроса (за IAvatarService и
/// IAvatarLoader) не допускает второй асинхронной операции, пока не закончилась первая.
/// </remarks>
public sealed class UserAvatarSource(IAvatarLoader avatarLoader, IAvatarService avatarService) : IUserAvatarSource, IDisposable
{
    private readonly SemaphoreSlim oneAtATime = new(1, 1);

    public async Task<AvatarInfo> GetAvatar(UserIdentification userId, AvatarIdentification? selectedAvatarId, int recommendedSize)
    {
        await oneAtATime.WaitAsync();
        try
        {
            var userAvatarId = selectedAvatarId ?? await avatarService.EnsureAvatarPresent(userId.Value);
            return await avatarLoader.GetAvatar(userAvatarId, recommendedSize);
        }
        finally
        {
            _ = oneAtATime.Release();
        }
    }

    public void Dispose() => oneAtATime.Dispose();
}
