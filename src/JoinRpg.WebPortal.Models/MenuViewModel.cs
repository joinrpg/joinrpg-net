using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.Web.Models;

public class MenuViewModelBase(ProjectInfo projectInfo, ICurrentUserAccessor currentUserAccessor, CharacterGroupLinkSlimViewModel[] bigGroups)
{
    public int ProjectId { get; } = projectInfo.ProjectId.Value;
    public string ProjectName { get; } = projectInfo.ProjectName;
    public bool EnableAccommodation { get; } = projectInfo.AccomodationEnabled;
    public CharacterGroupLinkSlimViewModel[] BigGroups { get; } = bigGroups;
    public bool IsAdmin { get; } = currentUserAccessor.IsAdmin;
    public bool ShowSchedule { get; } = projectInfo.ProjectScheduleSettings.ScheduleEnabled;
    public ProjectLifecycleStatus ProjectStatus { get; } = projectInfo.ProjectStatus;
}

public class PlayerMenuViewModel(ProjectInfo projectInfo, ICurrentUserAccessor currentUserAccessor, CharacterGroupLinkSlimViewModel[] bigGroups,
    IReadOnlyCollection<ClaimLinkViewModel> claims,
    IReadOnlyCollection<CaptainAccessRule> captainAccessRules)
    : MenuViewModelBase(projectInfo, currentUserAccessor, bigGroups)
{
    public IReadOnlyCollection<ClaimLinkViewModel> Claims { get; } = claims;
    public IReadOnlyCollection<CaptainAccessRule> CaptainAccessRules { get; } = captainAccessRules;
    public bool PlotPublished { get; } = projectInfo.PublishPlot;
}

public class MasterMenuViewModel(ProjectInfo projectInfo, ICurrentUserAccessor currentUserAccessor, CharacterGroupLinkSlimViewModel[] bigGroups, Permission[] permissions)
    : MenuViewModelBase(projectInfo, currentUserAccessor, bigGroups)
{
    public Permission[] Permissions { get; set; } = permissions;
    public bool CheckInModuleEnabled { get; } = projectInfo.ProjectCheckInSettings.CheckInModuleEnabled;
}
