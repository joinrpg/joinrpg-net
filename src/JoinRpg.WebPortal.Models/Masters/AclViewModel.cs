using JoinRpg.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Masters;

namespace JoinRpg.WebPortal.Models.Masters;

public class AclViewModel
{
    [Display(Name = "Мастер")]
    public UserProfileDetailsViewModel UserDetails { get; }

    public IReadOnlyCollection<CharacterGroupLinkSlimViewModel> ResponsibleFor { get; } = [];

    [Display(Name = "Проект")]
    public int ProjectId { get; }

    [Display(Name = "Игра")]
    public string ProjectName { get; }

    [Display(Name = "Заявок")]
    public int ClaimsCount { get; }

    public int UserId { get; }
    public PermissionBadgeViewModel[] Badges { get; set; }

    // Профиль мастера (ADR019, §4) — предзаполнение формы добавления (постится в AddAclViewModel)
    // и роль в таблице мастеров. Правка профиля — отдельно, на странице мастера.
    public string Role { get; set; } = AddAclViewModel.DefaultRole;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;

    /// <summary>Может ли текущий пользователь менять права (а не только профиль — свой профиль мастер правит сам).</summary>
    public bool CanEditPermissions { get; set; } = true;

    public bool CanGrantRights => Badges.Single(b => b.Permission == Permission.CanGrantRights).Value;

    // For Add/Edit pages: needs full User entity for detailed profile display
    public AclViewModel(ProjectInfo projectInfo, UserInfo targetUser, ICurrentUserAccessor currentUserAccessor)
    {
        UserDetails = new UserProfileDetailsViewModel(targetUser, projectInfo, currentUserAccessor);
        ProjectId = projectInfo.ProjectId;
        ProjectName = projectInfo.ProjectName;
        UserId = targetUser.UserId;
        Badges = projectInfo.GetPermissionViewModels(targetUser.UserId);
    }

    // For master list and manage pages
    public AclViewModel(ProjectMasterInfo master, int claimsCount, ProjectInfo projectInfo)
    {
        UserDetails = new UserProfileDetailsViewModel(master.UserInfo);
        UserId = master.UserId.Value;
        ProjectId = projectInfo.ProjectId;
        ProjectName = projectInfo.ProjectName;
        Badges = projectInfo.GetPermissionViewModels(master.UserId);
        ClaimsCount = claimsCount;
        ResponsibleFor = [.. projectInfo.GroupTree.ResponsibleMasterRules
            .Where(g => g.ResponsibleMasterId == master.UserId && g.IsActive)
            .Select(g => new CharacterGroupLinkSlimViewModel(g))];
    }
}
