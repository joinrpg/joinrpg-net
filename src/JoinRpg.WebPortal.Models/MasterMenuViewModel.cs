using JoinRpg.DataModel;
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
}

public class PlayerMenuViewModel : MenuViewModelBase
{
    public ICollection<ClaimShortListItemViewModel> Claims { get; set; }
    public bool PlotPublished { get; set; }
}

public class MasterMenuViewModel : MenuViewModelBase
{
    public ProjectAcl AccessToProject { get; set; }
    public bool CheckInModuleEnabled { get; set; }
}
