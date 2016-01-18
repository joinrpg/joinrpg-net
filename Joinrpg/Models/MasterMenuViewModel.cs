using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class MasterMenuViewModel
  {
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }

    public ProjectAcl AccessToProject { get; set; }

    public IEnumerable<CharacterGroupLinkViewModel> BigGroups { get; set; }

    public bool IsActive { get; set; }

    public bool IsAcceptingClaims { get; set; }
  }
}
