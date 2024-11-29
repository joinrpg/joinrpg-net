using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;

namespace JoinRpg.Web.Models.Masters;

public class AclViewModel(Project project, User targetUser, User currentUser, PermissionBadgeViewModel[] bagdes)
{
    [Display(Name = "Мастер")]
    public UserProfileDetailsViewModel UserDetails { get; } = new UserProfileDetailsViewModel(targetUser, currentUser);

    public IReadOnlyCollection<GameObjectLinkViewModel> ResponsibleFor { get; } = [];

    public int? ProjectAclId { get; }

    [Display(Name = "Проект")]
    public int ProjectId { get; } = project.ProjectId;

    [Display(Name = "Игра")]
    public string ProjectName { get; } = project.ProjectName;

    [Display(Name = "Заявок")]
    public int ClaimsCount { get; } = 0;

    public int UserId { get; } = targetUser.UserId;
    public PermissionBadgeViewModel[] Badges { get; set; } = bagdes;

    public bool CanGrantRights => Badges.Single(b => b.Permission == Permission.CanGrantRights).Value;

    public AclViewModel(ProjectAcl acl,
        User currentUser,
        int claimsCount,
        IEnumerable<CharacterGroup> groups,
        IUriService uriService)
        : this(acl.Project, acl.User, currentUser, acl.GetPermissionViewModels())
    {
        ProjectAclId = acl.ProjectAclId;
        ClaimsCount = claimsCount;
        ResponsibleFor = groups?.AsObjectLinks(uriService).ToArray() ?? [];
    }
}
