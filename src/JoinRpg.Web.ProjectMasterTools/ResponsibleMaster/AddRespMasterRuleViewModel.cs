using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;
public class AddRespMasterRuleViewModel
{
    [Required]
    public CharacterGroupDto Group { get; set; } = null!;

    [Required]
    public MasterViewModel Master { get; set; } = null!;
}
