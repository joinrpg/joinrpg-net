using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.Access;

namespace JoinRpg.Web.Models;

public class ErrorNoAccessToProjectViewModel
{
    public string ProjectName { get; }
    public int ProjectId { get; }
    public IEnumerable<User> CanGrantAccess { get; }
    public Permission Permission { get; }

    public ErrorNoAccessToProjectViewModel(Project project, Permission permission = Permission.None)
    {
        ArgumentNullException.ThrowIfNull(project);

        CanGrantAccess = project.ProjectAcls.Where(acl => acl.CanGrantRights).Select(acl => acl.User);
        ProjectId = project.ProjectId;
        ProjectName = project.ProjectName;
        Permission = permission;
    }
}
