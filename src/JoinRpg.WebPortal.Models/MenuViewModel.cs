using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.ProjectCommon;

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
    IReadOnlyCollection<ClaimShortListItemViewModel> claims)
    : MenuViewModelBase(projectInfo, currentUserAccessor, bigGroups)
{
    public IReadOnlyCollection<ClaimShortListItemViewModel> Claims { get; } = claims;
    public bool PlotPublished { get; } = projectInfo.PublishPlot;
}

public class MasterMenuViewModel(ProjectInfo projectInfo, ICurrentUserAccessor currentUserAccessor, CharacterGroupLinkSlimViewModel[] bigGroups, Permission[] permissions)
    : MenuViewModelBase(projectInfo, currentUserAccessor, bigGroups)
{
    public Permission[] Permissions { get; set; } = permissions;
    public bool CheckInModuleEnabled { get; } = projectInfo.ProjectCheckInSettings.CheckInModuleEnabled;
}
