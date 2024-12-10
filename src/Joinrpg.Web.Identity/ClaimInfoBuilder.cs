using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using Claim = System.Security.Claims.Claim;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace Joinrpg.Web.Identity;

internal static class ClaimInfoBuilder
{
    public static IEnumerable<Claim> ToClaimsList(this User dbUser)
    {
        yield return new Claim(ClaimTypes.Email, dbUser.Email);
        yield return new Claim(JoinClaimTypes.DisplayName, dbUser.GetDisplayName());
        if (dbUser.SelectedAvatarId is not null)
        {
            //TODO: When we fix all avatars, it will be not required check
            yield return new Claim(JoinClaimTypes.AvatarId, dbUser.SelectedAvatarId?.ToString());
        }

        foreach (var acl in dbUser.ProjectAcls)
        {
            var permission = acl.GetPermissions();
            yield return new Claim(PermissionEncoder.GetProjectPermissionClaimName(acl.ProjectId), PermissionEncoder.Encode(permission));
        }
    }
}
