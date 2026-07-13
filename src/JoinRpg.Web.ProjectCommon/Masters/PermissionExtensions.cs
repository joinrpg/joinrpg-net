using System.Diagnostics.Contracts;

namespace JoinRpg.Web.ProjectCommon.Masters;

public static class PermissionExtensions
{
    [Pure]
    public static PermissionBadgeViewModel[] GetPermissionViewModels(this ProjectInfo project, UserIdentification userId)
    {
        var acl = project.GetMasterAccess(userId);
        return Enum.GetValues<Permission>()
            .Select(permission => new PermissionBadgeViewModel(permission, acl.Contains(permission)))
            .Where(badge => !badge.IsNone)
            .Where(badge => !badge.OnlyIfAccommodationEnabled || project.AccomodationEnabled)
            .ToArray();
    }
}
