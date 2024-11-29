using System.Diagnostics.Contracts;
using JoinRpg.DataModel;
using JoinRpg.Domain.Access;
using JoinRpg.PrimitiveTypes.Access;

namespace JoinRpg.Web.Models.Masters;

public static class PermissionExtensions
{
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
    public static PermissionBadgeViewModel[] GetEmptyPermissionViewModels(this Project project)
    {
        return Enum.GetValues<Permission>()
            .Select(permission => new PermissionBadgeViewModel(permission, false))
            .Where(badge => !badge.IsNone)
            .Where(badge => !badge.OnlyIfAccommodationEnabled || project.Details.EnableAccommodation)
            .ToArray();
    }
}
