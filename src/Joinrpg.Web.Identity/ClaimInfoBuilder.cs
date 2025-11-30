using JoinRpg.DataModel;
using JoinRpg.Domain;
using Claim = System.Security.Claims.Claim;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace Joinrpg.Web.Identity;

internal static class ClaimInfoBuilder
{
    public static IEnumerable<Claim> ToClaimsList(this User dbUser)
    {
        yield return new Claim(ClaimTypes.Email, dbUser.Email);
        var displayName = dbUser.ExtractDisplayName();
        yield return new Claim(JoinClaimTypes.DisplayName, displayName.DisplayName);
        if (displayName.FullName is not null)
        {
            yield return new Claim(JoinClaimTypes.FullName, displayName.FullName);
        }
        if (dbUser.SelectedAvatarId is not null)
        {
            //TODO: When we fix all avatars, it will be not required check
            yield return new Claim(JoinClaimTypes.AvatarId, dbUser.SelectedAvatarId.ToString()!);
        }
    }
}
