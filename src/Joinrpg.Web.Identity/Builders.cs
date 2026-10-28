using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;

namespace Joinrpg.Web.Identity;

internal static class Builders
{
    public static JoinIdentityUser ToIdentityUser(this User dbUser)
        => new()
        {
            UserName = dbUser.UserName,
            Id = dbUser.UserId,
            HasPassword = dbUser.PasswordHash != null,
            EmaiLConfirmed = dbUser.Auth.EmailConfirmed,
            PasswordHash = dbUser.PasswordHash,
            DisplayName = dbUser.ExtractDisplayName(),
        };
}
