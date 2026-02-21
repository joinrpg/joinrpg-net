using JoinRpg.DataModel;

namespace Joinrpg.Web.Identity.AspNetCore;

internal static class Builders
{
    public static UserExternalLogin ToUserExternalLogin(this UserLoginInfo login)
        => new()
        {
            Key = login.ProviderKey,
            Provider = login.LoginProvider,
        };

    public static UserLoginInfo ToUserLoginInfoCore(this UserExternalLogin uel)
        => new(uel.Provider, uel.Key, uel.Key);
}
