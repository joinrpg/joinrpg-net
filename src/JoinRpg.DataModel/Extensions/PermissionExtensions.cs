using System.Diagnostics.Contracts;
using JoinRpg.PrimitiveTypes.Access;

namespace JoinRpg.DataModel.Extensions;

public static class PermissionExtensions
{
    [Pure]
    public static Func<ProjectAcl, bool> GetPermssionExpression(this Permission permission)
    {
        return permission switch
        {
            Permission.None => acl => true,
            Permission.CanChangeFields => acl => acl.CanChangeFields,
            Permission.CanChangeProjectProperties => acl => acl.CanChangeProjectProperties,
            Permission.CanGrantRights => acl => acl.CanGrantRights,
            Permission.CanManageClaims => acl => acl.CanManageClaims,
            Permission.CanEditRoles => acl => acl.CanEditRoles,
            Permission.CanManageMoney => acl => acl.CanManageMoney,
            Permission.CanSendMassMails => acl => acl.CanSendMassMails,
            Permission.CanManagePlots => acl => acl.CanManagePlots,
            Permission.CanManageAccommodation => acl => acl.CanManageAccommodation,
            Permission.CanSetPlayersAccommodations => acl => acl.CanSetPlayersAccommodations,
            _ => throw new ArgumentOutOfRangeException(nameof(permission)),
        };
    }

    [Pure]
    public static Permission[] GetPermissions(this ProjectAcl acl)
    {
        return Impl().ToArray();

        IEnumerable<Permission> Impl()
        {
            foreach (var permission in Enum.GetValues<Permission>())
            {
                if (permission.GetPermssionExpression()(acl))
                {
                    yield return permission;
                }
            }
        }
    }
}
