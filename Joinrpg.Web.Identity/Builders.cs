
using JoinRpg.DataModel;
using Microsoft.AspNet.Identity;

namespace Joinrpg.Web.Identity
{
    internal static class Builders
    {
        public static IdentityUser ToIdentityUser(this User dbUser)
            => new IdentityUser()
            {
                UserName = dbUser.UserName,
                Id = dbUser.UserId,
                HasPassword = dbUser.PasswordHash != null,
            };

        public static UserExternalLogin ToUserExternalLogin(this UserLoginInfo login)
            => new UserExternalLogin()
            {
                Key = login.ProviderKey,
                Provider = login.LoginProvider,
            };

        public static UserLoginInfo ToUserLoginInfo(this UserExternalLogin uel)
            => new UserLoginInfo(uel.Provider, uel.Key);
    }
}
