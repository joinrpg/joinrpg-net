using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models;

public class MenuViewModelBase
{
    // Constructed only in SetCommonMenuParameters and it's ensures that every member is set
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = null!;
    public bool IsActive { get; set; }
    public bool IsAcceptingClaims { get; set; }
    public bool EnableAccommodation { get; set; }
    public IEnumerable<CharacterGroupLinkSlimViewModel> BigGroups { get; set; } = null!;
    public bool IsAdmin { get; set; }
    public bool ShowSchedule { get; set; }
    public ProjectLifecycleStatus ProjectStatus { get; set; }
}

public class PlayerMenuViewModel : MenuViewModelBase
{
    public required IReadOnlyCollection<ClaimShortListItemViewModel> Claims { get; set; }
    public required bool PlotPublished { get; set; }
}

public class MasterMenuViewModel : MenuViewModelBase
{
    public required IReadOnlyCollection<Permission> Permissions { get; set; }
    public required bool CheckInModuleEnabled { get; set; }
}
