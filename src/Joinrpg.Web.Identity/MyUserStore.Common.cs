using Microsoft.EntityFrameworkCore;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using DbUser = JoinRpg.DataModel.User;

namespace Joinrpg.Web.Identity;

public partial class MyUserStore
{
    private readonly MyDbContext _ctx;
    private readonly DbSet<DbUser> _userSet;

    public MyUserStore(MyDbContext ctx)
    {
        _ctx = ctx;
        _userSet = _ctx.Set<DbUser>();
    }

    /// <inheritedoc />
    void IDisposable.Dispose() => _ctx?.Dispose();

    private async Task CreateImpl(JoinIdentityUser user, CancellationToken ct = default)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var hasAnyUser = await _userSet.AnyAsync(ct);

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

        _ = _ctx.UserSet.Add(dbUser);
        _ = await _ctx.SaveChangesAsync(ct);
        user.Id = dbUser.UserId;
    }

    private async Task UpdateImpl(JoinIdentityUser user, CancellationToken ct = default)
    {
        var dbUser = await LoadUser(user, ct);
        dbUser.UserName = user.UserName;
        dbUser.Email = user.UserName;
        dbUser.Auth.EmailConfirmed = user.EmaiLConfirmed;
        dbUser.PasswordHash = user.PasswordHash;
        _ = await _ctx.SaveChangesAsync(ct);
    }

    private async Task SetUserNameImpl(JoinIdentityUser user, string email, CancellationToken ct = default)
    {
        var dbUser = await LoadUser(user, ct);
        dbUser.Email = email;
        dbUser.UserName = email;
        _ = await _ctx.SaveChangesAsync(ct);
    }

    [ItemCanBeNull]
    private async Task<DbUser> LoadUser(string userName, CancellationToken ct = default) =>
        await _ctx.UserSet.SingleOrDefaultAsync(user => user.Email == userName, ct);

    [ItemCanBeNull]
    private async Task<DbUser> LoadUser(int id, CancellationToken ct = default) =>
        await _ctx.UserSet.SingleOrDefaultAsync(user => user.UserId == id, ct);

    [ItemCanBeNull]
    private async Task<DbUser> LoadUser(JoinIdentityUser joinIdentityUser, CancellationToken ct = default) =>
       await _ctx.UserSet.Include(u => u.ExternalLogins).SingleOrDefaultAsync(user => user.UserId == joinIdentityUser.Id, ct);

}
