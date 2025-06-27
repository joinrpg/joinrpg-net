using System.Data.Entity;
using Joinrpg.Web.Identity.AspNetCore;
using JoinRpg.DataModel;

namespace Joinrpg.Web.Identity;

internal partial class MyUserStore : IUserLoginStore<JoinIdentityUser>, ICustomLoginStore
{
    async Task ICustomLoginStore.AddCustomLoginAsync(JoinIdentityUser user, string key, string provider, CancellationToken ct)
    {
        var dbUser = await LoadUser(user, ct);
        dbUser.ExternalLogins.Add(new UserExternalLogin() { Key = key, Provider = provider });
        _ = await ctx.SaveChangesAsync(ct);
    }

    async Task IUserLoginStore<JoinIdentityUser>.AddLoginAsync(JoinIdentityUser user, UserLoginInfo login, CancellationToken cancellationToken)
    {
        var dbUser = await LoadUser(user, cancellationToken);
        dbUser.ExternalLogins.Add(login.ToUserExternalLogin());
        _ = await ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task<JoinIdentityUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        var uel = await ctx.Set<UserExternalLogin>().SingleOrDefaultAsync(u => u.Key == providerKey && u.Provider == loginProvider);

        return uel?.User.ToIdentityUser();
    }

    async Task<IList<UserLoginInfo>> IUserLoginStore<JoinIdentityUser>.GetLoginsAsync(JoinIdentityUser user, CancellationToken cancellationToken)
    {
        var dbUser = await LoadUser(user, cancellationToken);
        return [.. dbUser.ExternalLogins.Select(uel => uel.ToUserLoginInfoCore())];
    }

    async Task IUserLoginStore<JoinIdentityUser>.RemoveLoginAsync(JoinIdentityUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        var dbUser = await LoadUser(user, cancellationToken);
        var el =
            dbUser.ExternalLogins.First(
                externalLogin => externalLogin.Key == providerKey &&
                                 externalLogin.Provider.Equals(loginProvider, StringComparison.InvariantCultureIgnoreCase));
        _ = ctx.Set<UserExternalLogin>().Remove(el);
        _ = await ctx.SaveChangesAsync(cancellationToken);
    }
}

public interface ICustomLoginStore
{
    Task AddCustomLoginAsync(JoinIdentityUser user, string key, string provider, CancellationToken ct);
    Task<JoinIdentityUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken);
}
