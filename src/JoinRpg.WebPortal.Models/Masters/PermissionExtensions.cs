using System.Diagnostics.Contracts;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models.Masters;

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

    [Pure]
    public static PermissionBadgeViewModel[] GetPermissionViewModels(this ProjectAcl acl)
    {
        return Enum.GetValues<Permission>()
            .Select(permission => new PermissionBadgeViewModel(permission, permission.GetPermssionExpression()(acl)))
            .Where(badge => !badge.IsNone)
            .Where(badge => !badge.OnlyIfAccommodationEnabled || acl.Project.Details.EnableAccommodation)
            .ToArray();
    }

    [Pure]
    public static PermissionBadgeViewModel[] GetEmptyPermissionViewModels(this ProjectInfo project)
    {
        return Enum.GetValues<Permission>()
            .Select(permission => new PermissionBadgeViewModel(permission, false))
            .Where(badge => !badge.IsNone)
            .Where(badge => !badge.OnlyIfAccommodationEnabled || project.AccomodationEnabled)
            .ToArray();
    }
}
