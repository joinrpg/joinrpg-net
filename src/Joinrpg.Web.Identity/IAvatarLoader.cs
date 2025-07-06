using JoinRpg.PrimitiveTypes;

namespace Joinrpg.Web.Identity;

public interface IAvatarLoader
{
    Task<AvatarInfo> GetAvatar(AvatarIdentification userAvatarId, int recommendedSize);
}
