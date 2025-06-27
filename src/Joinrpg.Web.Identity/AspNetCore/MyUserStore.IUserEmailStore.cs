namespace Joinrpg.Web.Identity;

internal partial class MyUserStore : IUserEmailStore<JoinIdentityUser>
{
    async Task<JoinIdentityUser?> IUserEmailStore<JoinIdentityUser>.FindByEmailAsync(string normalizedEmail, CancellationToken ct)
    {
        var dbUser = await TryLoadUser(normalizedEmail, ct);
        return dbUser?.ToIdentityUser();
    }

    Task<string?> IUserEmailStore<JoinIdentityUser>.GetEmailAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult((string?)user.UserName);

    async Task<bool> IUserEmailStore<JoinIdentityUser>.GetEmailConfirmedAsync(JoinIdentityUser user, CancellationToken ct)
    {
        var dbUser = await LoadUser(user, ct);
        return dbUser.Auth.EmailConfirmed;
    }

    Task<string?> IUserEmailStore<JoinIdentityUser>.GetNormalizedEmailAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult((string?)user.UserName.ToUpperInvariant());

    Task IUserEmailStore<JoinIdentityUser>.SetEmailAsync(JoinIdentityUser user, string? email, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        //it will be saved by calling Update later
        user.UserName = email;
        return Task.CompletedTask;
    }

    Task IUserEmailStore<JoinIdentityUser>.SetEmailConfirmedAsync(JoinIdentityUser user, bool confirmed, CancellationToken ct)
    {
        //it will be saved by calling Update later
        user.EmaiLConfirmed = confirmed;
        return Task.CompletedTask;
    }

    Task IUserEmailStore<JoinIdentityUser>.SetNormalizedEmailAsync(JoinIdentityUser user, string? normalizedEmail, CancellationToken ct) =>
        //We don't persist it for now
        Task.CompletedTask;
}
