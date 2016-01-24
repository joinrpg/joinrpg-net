using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class MenuViewModelBase
  {
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }
    public bool IsActive { get; set; }
    public bool IsAcceptingClaims { get; set; }
    public int? CurrentUserId { get; set; }
  }

  public class PlayerMenuViewModel : MenuViewModelBase
  {
    public ICollection<ClaimShortListItemViewModel> Claims { get; set; }
    public int? RootGroupId { get; internal set; }
  }
  public class MasterMenuViewModel : MenuViewModelBase
  {
    public ProjectAcl AccessToProject { get; set; }

    public IEnumerable<CharacterGroupLinkViewModel> BigGroups { get; set; }
  }
}
