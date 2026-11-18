using System.ComponentModel.DataAnnotations;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;

public class AddRespMasterRuleViewModel
{
    [Required]
    public CharacterGroupDto Group { get; set; } = null!;

    [Required]
    public UserInfoHeader Master { get; set; } = null!;
}
