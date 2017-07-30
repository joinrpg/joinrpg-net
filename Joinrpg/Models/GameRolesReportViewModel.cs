using System.Collections.Generic;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Models
{
  public class GameRolesReportViewModel
  {
    public int ProjectId { get; set; }

    public IEnumerable<CharacterGroupReportItemViewModel> Data { get; set; }

    public CharacterGroupDetailsViewModel Details { get; set; }
    public bool CheckinModuleEnabled { get; set; }
  }
}