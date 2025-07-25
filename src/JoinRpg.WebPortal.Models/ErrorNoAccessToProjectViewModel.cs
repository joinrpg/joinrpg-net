using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models;

public class ErrorNoAccessToProjectViewModel
{
    public string ProjectName { get; }
    public int ProjectId { get; }
    public IEnumerable<UserLinkViewModel> CanGrantAccess { get; }
    public Permission Permission { get; }

    public ErrorNoAccessToProjectViewModel(ProjectInfo project, Permission permission = Permission.None)
    {
        ArgumentNullException.ThrowIfNull(project);

        CanGrantAccess = project.Masters.Where(master => master.Permissions.Contains(Permission.CanGrantRights)).Select(master => master.ToUserLinkViewModel());
        ProjectId = project.ProjectId;
        ProjectName = project.ProjectName;
        Permission = permission;
    }
}
