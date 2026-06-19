using JoinRpg.DomainTypes.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.Web.Models;

public class MenuViewModelBase(ProjectInfo projectInfo, ICurrentUserAccessor currentUserAccessor)
{
    public int ProjectId { get; } = projectInfo.ProjectId.Value;
    public string ProjectName { get; } = projectInfo.ProjectName;
    public bool EnableAccommodation { get; } = projectInfo.AccomodationEnabled;
    public bool IsAdmin { get; } = currentUserAccessor.IsAdmin;
    public bool ShowSchedule { get; } = projectInfo.ProjectScheduleSettings.ScheduleEnabled;
    public ProjectLifecycleStatus ProjectStatus { get; } = projectInfo.ProjectStatus;
    public IReadOnlyCollection<ProjectRolesList> ProjectRolesLists { get; } = projectInfo.ProjectRolesLists;

    protected static IEnumerable<CharacterGroupLinkSlimViewModel> LoadBigGroups(ProjectInfo projectInfo)
    {
        return projectInfo.GetDirectChildGroups(projectInfo.RootCharacterGroupId)
            .Select(dto => new CharacterGroupLinkSlimViewModel(dto.Id, dto.Name, dto.IsPublic, dto.IsActive));
    }
}

public class PlayerMenuViewModel(ProjectInfo projectInfo, ICurrentUserAccessor currentUserAccessor, IReadOnlyCollection<ClaimLinkViewModel> claims,
    IReadOnlyCollection<CaptainAccessRule> captainAccessRules)
    : MenuViewModelBase(projectInfo, currentUserAccessor)
{
    public IReadOnlyCollection<ClaimLinkViewModel> Claims { get; } = claims;
    public IReadOnlyCollection<CaptainAccessRule> CaptainAccessRules { get; } = captainAccessRules;
    public bool PlotPublished { get; } = projectInfo.PublishPlot;

    public CharacterGroupLinkSlimViewModel[] BigGroups { get; } = [.. LoadBigGroups(projectInfo).Where(x => x.IsPublic)];
}

public class MasterMenuViewModel(ProjectInfo projectInfo, ICurrentUserAccessor currentUserAccessor, Permission[] permissions)
    : MenuViewModelBase(projectInfo, currentUserAccessor)
{
    public Permission[] Permissions { get; set; } = permissions;
    public bool CheckInModuleEnabled { get; } = projectInfo.ProjectCheckInSettings.CheckInModuleEnabled;
    public CharacterGroupLinkSlimViewModel[] BigGroups { get; } = [.. LoadBigGroups(projectInfo)];

}
