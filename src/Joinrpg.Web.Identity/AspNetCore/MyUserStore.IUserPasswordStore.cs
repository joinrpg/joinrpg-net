namespace Joinrpg.Web.Identity;

internal partial class MyUserStore : IUserPasswordStore<JoinIdentityUser>
{
    Task<string?> IUserPasswordStore<JoinIdentityUser>.GetPasswordHashAsync(JoinIdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash);

    Task<bool> IUserPasswordStore<JoinIdentityUser>.HasPasswordAsync(JoinIdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.HasPassword);

    Task IUserPasswordStore<JoinIdentityUser>.SetPasswordHashAsync(JoinIdentityUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }
}
