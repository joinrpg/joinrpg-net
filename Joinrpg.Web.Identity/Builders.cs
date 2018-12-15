using Joinrpg.Web.Identity;
using JoinRpg.DataModel;
using Microsoft.AspNet.Identity;

internal static class Builders
{
    public static IdentityUser ToIdentityUser(this User dbUser)
        => new IdentityUser()
        {
            UserName = dbUser.UserName,
            Id = dbUser.UserId,
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
