using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Joinrpg.Web.Identity;

public partial class MyUserStore : IUserClaimStore<JoinIdentityUser>
{
    Task IUserClaimStore<JoinIdentityUser>.AddClaimsAsync(JoinIdentityUser user, IEnumerable<Claim> claims, CancellationToken ct) => throw new NotSupportedException();

    async Task<IList<Claim>> IUserClaimStore<JoinIdentityUser>.GetClaimsAsync(JoinIdentityUser user, CancellationToken ct)
    {
        var dbUser = await LoadUser(user, ct);
        return dbUser.ToClaimsList();
    }

    Task<IList<JoinIdentityUser>> IUserClaimStore<JoinIdentityUser>.GetUsersForClaimAsync(Claim claim, CancellationToken ct) => throw new NotSupportedException();

    Task IUserClaimStore<JoinIdentityUser>.RemoveClaimsAsync(JoinIdentityUser user, IEnumerable<Claim> claims, CancellationToken ct) => throw new NotSupportedException();

    Task IUserClaimStore<JoinIdentityUser>.ReplaceClaimAsync(JoinIdentityUser user, Claim claim, Claim newClaim, CancellationToken ct) => throw new NotSupportedException();
}
