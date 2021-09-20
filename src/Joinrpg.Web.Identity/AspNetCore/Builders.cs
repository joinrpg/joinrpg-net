
using JetBrains.Annotations;
using JoinRpg.DataModel;
using Microsoft.AspNetCore.Identity;

namespace Joinrpg.Web.Identity.AspNetCore
{
    internal static class Builders
    {
        public static UserExternalLogin ToUserExternalLogin([NotNull] this UserLoginInfo login)
            => new()
        {
            Key = login.ProviderKey,
            Provider = login.LoginProvider,
        };

        public static UserLoginInfo ToUserLoginInfoCore([NotNull] this UserExternalLogin uel)
            => new(uel.Provider, uel.Key, uel.Key);
    }
}
