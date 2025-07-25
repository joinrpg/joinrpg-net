namespace Joinrpg.Web.Identity;

internal partial class MyUserStore : IUserStore<JoinIdentityUser>
{
    async Task<IdentityResult> IUserStore<JoinIdentityUser>.CreateAsync(JoinIdentityUser user, CancellationToken ct)
    {
        await CreateImpl(user, ct);
        return IdentityResult.Success;
    }

    Task<IdentityResult> IUserStore<JoinIdentityUser>.DeleteAsync(JoinIdentityUser user, CancellationToken ct) => throw new NotSupportedException();

    async Task<JoinIdentityUser?> IUserStore<JoinIdentityUser>.FindByIdAsync(string userId, CancellationToken ct)
    {
        if (int.TryParse(userId, out var intId))
        {
            var dbUser = await TryLoadUser(intId, ct);
            return dbUser?.ToIdentityUser();
        }
        else
        {
            return null;
        }
    }

    async Task<JoinIdentityUser?> IUserStore<JoinIdentityUser>.FindByNameAsync(string normalizedUserName, CancellationToken ct)
    {
        var dbUser = await TryLoadUser(normalizedUserName, ct);
        return dbUser?.ToIdentityUser();
    }


    Task<string?> IUserStore<JoinIdentityUser>.GetNormalizedUserNameAsync(JoinIdentityUser user, CancellationToken ct)
        => Task.FromResult((string?)user.UserName.ToUpperInvariant());


    Task<string> IUserStore<JoinIdentityUser>.GetUserIdAsync(JoinIdentityUser user, CancellationToken ct)
        => Task.FromResult(user.Id.ToString());

    Task<string?> IUserStore<JoinIdentityUser>.GetUserNameAsync(JoinIdentityUser user, CancellationToken ct)
        => Task.FromResult((string?)user.UserName);

    Task IUserStore<JoinIdentityUser>.SetNormalizedUserNameAsync(JoinIdentityUser user, string? normalizedName, CancellationToken ct)
    {
        //TODO persist it and start searching by it
        return Task.CompletedTask;
    }

    async Task IUserStore<JoinIdentityUser>.SetUserNameAsync(JoinIdentityUser user, string? userName, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        await SetUserNameImpl(user, userName, ct);
    }

    async Task<IdentityResult> IUserStore<JoinIdentityUser>.UpdateAsync(JoinIdentityUser user, CancellationToken ct)
    {
        await UpdateImpl(user, ct);
        return IdentityResult.Success;
    }
}
