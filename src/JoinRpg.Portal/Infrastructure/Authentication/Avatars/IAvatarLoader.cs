using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Portal.Infrastructure.Authentication.Avatars
{
#nullable enable
    public interface IAvatarLoader
    {
        Task<AvatarInfo> GetAvatar(AvatarIdentification userAvatarId, int recommendedSize);
    }
}
