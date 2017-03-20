using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Models
{
  public class MenuViewModelBase
  {
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }
    public bool IsActive { get; set; }
    public bool IsAcceptingClaims { get; set; }
    public int? RootGroupId { get; internal set; }
    public IEnumerable<CharacterGroupLinkViewModel> BigGroups { get; set; }
    public bool IsAdmin { get; set; }
  }

  public class PlayerMenuViewModel : MenuViewModelBase
  {
    public ICollection<ClaimShortListItemViewModel> Claims { get; set; }
    public bool PlotPublished { get; set; }
  }

  public class MasterMenuViewModel : MenuViewModelBase
  {
    public ProjectAcl AccessToProject { get; set; }
  }
}
