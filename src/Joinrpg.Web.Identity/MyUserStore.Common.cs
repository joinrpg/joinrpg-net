using System.Data.Entity;
using System.Linq.Expressions;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using DbUser = JoinRpg.DataModel.User;

namespace Joinrpg.Web.Identity;

public partial class MyUserStore(MyDbContext ctx)
{

    /// <inheritedoc />
    void IDisposable.Dispose() => ctx?.Dispose();

    private async Task CreateImpl(JoinIdentityUser user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var hasAnyUser = await ctx.Set<DbUser>().AnyAsync(ct);

        var dbUser = new DbUser()
        {
            UserName = user.UserName,
            Email = user.UserName,
            Auth = new UserAuthDetails()
            {
                RegisterDate = DateTime.UtcNow,
                AspNetSecurityStamp = "",
            },
            PasswordHash = user.PasswordHash,
            Extra = new UserExtra
            {
                SocialNetworksAccess = ContactsAccessType.Public,
            },
        };

        if (!hasAnyUser)
        {
            dbUser.Auth.EmailConfirmed = true;
            dbUser.Auth.IsAdmin = true;
        }

        _ = ctx.UserSet.Add(dbUser);
        _ = await ctx.SaveChangesAsync(ct);
        user.Id = dbUser.UserId;
    }

    private async Task UpdateImpl(JoinIdentityUser user, CancellationToken ct = default)
    {
        var dbUser = await LoadUser(user, ct);
        dbUser.UserName = user.UserName;
        dbUser.Email = user.UserName;
        dbUser.Auth.EmailConfirmed = user.EmaiLConfirmed;
        dbUser.PasswordHash = user.PasswordHash;
        _ = await ctx.SaveChangesAsync(ct);
    }

    private async Task SetUserNameImpl(JoinIdentityUser user, string email, CancellationToken ct = default)
    {
        var dbUser = await LoadUser(user, ct);
        dbUser.Email = email;
        dbUser.UserName = email;
        _ = await ctx.SaveChangesAsync(ct);
    }

    private async Task<DbUser?> LoadUser(string userName, CancellationToken ct = default) =>
        await LoadUser(user => user.Email == userName, ct);

    private async Task<DbUser?> LoadUser(int id, CancellationToken ct = default) =>
        await LoadUser(user => user.UserId == id, ct);

    private async Task<DbUser?> LoadUser(JoinIdentityUser joinIdentityUser, CancellationToken ct = default) =>
       await LoadUser(user => user.UserId == joinIdentityUser.Id, ct);

    private async Task<DbUser?> LoadUser(Expression<Func<DbUser, bool>> predicate, CancellationToken ct) =>
       await ctx
        .Set<DbUser>()
        .Include(u => u.ExternalLogins)
        .Include(u => u.ProjectAcls)
        .SingleOrDefaultAsync(predicate, ct);

}
