
using JetBrains.Annotations;
using JoinRpg.DataModel;
using Microsoft.AspNet.Identity;

namespace Joinrpg.Web.Identity
{
    internal static class Builders
    {
        public static JoinIdentityUser ToIdentityUser([NotNull] this User dbUser)
            => new JoinIdentityUser()
            {
                UserName = dbUser.UserName,
                Id = dbUser.UserId,
                HasPassword = dbUser.PasswordHash != null,
            };

        public static UserExternalLogin ToUserExternalLogin([NotNull] this UserLoginInfo login)
            => new UserExternalLogin()
            {
                Key = login.ProviderKey,
                Provider = login.LoginProvider,
            };

        public static UserLoginInfo ToUserLoginInfo([NotNull] this UserExternalLogin uel)
            => new UserLoginInfo(uel.Provider, uel.Key);
    }
}
