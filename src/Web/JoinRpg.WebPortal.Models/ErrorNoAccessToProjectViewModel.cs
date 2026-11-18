using JoinRpg.Common.WebComponents;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Masters;

namespace JoinRpg.Web.Models;

public static class NoAccessToProjectViewModelBuilder
{
    /// <param name="viewer">Кому показываем: непубличных мастеров видят только мастера проекта (ADR019, §3).</param>
    public static NoAccessToProjectViewModel Build(ProjectInfo project, UserIdentification? viewer, Permission permission = Permission.None)
    {
        ArgumentNullException.ThrowIfNull(project);

        return new NoAccessToProjectViewModel(
            new ProjectIdentification(project.ProjectId),
            project.ProjectName,
            permission == Permission.None ? null : new PermissionBadgeViewModel(permission, Value: false),
            [.. project.GetMastersVisibleTo(viewer)
                .Where(master => master.Permissions.Contains(Permission.CanGrantRights))
                .Select(master => new UserLinkViewModel(master.UserInfo))]);
    }
}
