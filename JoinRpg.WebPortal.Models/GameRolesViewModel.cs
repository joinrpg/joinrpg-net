using System.Collections.Generic;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Models
{
  public class GameRolesViewModel
  {
    public int ProjectId { get; set; }

    public string ProjectName{ get; set; }

    public bool ShowEditControls { get; set; }
    public bool HasMasterAccess { get; set; }
    public IEnumerable<CharacterGroupListItemViewModel> Data { get; set; }

    public CharacterGroupDetailsViewModel Details { get; set; }
  }
}
