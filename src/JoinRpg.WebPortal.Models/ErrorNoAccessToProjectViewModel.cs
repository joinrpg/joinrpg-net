using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Masters;

namespace JoinRpg.Web.Models;

public static class NoAccessToProjectViewModelBuilder
{
    public static NoAccessToProjectViewModel Build(ProjectInfo project, Permission permission = Permission.None)
    {
        ArgumentNullException.ThrowIfNull(project);

        return new NoAccessToProjectViewModel(
            new ProjectIdentification(project.ProjectId),
            project.ProjectName,
            permission == Permission.None ? null : new PermissionBadgeViewModel(permission, Value: false),
            [.. project.Masters
                .Where(master => master.Permissions.Contains(Permission.CanGrantRights))
                .Select(master => master.ToUserLinkViewModel())]);
    }
}
