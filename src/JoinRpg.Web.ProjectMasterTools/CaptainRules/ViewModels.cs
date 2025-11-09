using System.ComponentModel.DataAnnotations;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.Web.ProjectMasterTools.CaptainRules;
public record CaptainRuleViewModel(CaptainAccessRule Rule, string GroupName, UserLinkViewModel PlayerLink);

public record CaptainRuleListViewModel(List<CaptainRuleViewModel> Items, bool HasEditAccess);

public class AddCaptainRuleViewModel
{
    [Required]
    public CharacterGroupDto Group { get; set; } = null!;

    [Required]
    public ClaimLinkViewModel Claim { get; set; } = null!;
    public bool CanApprove { get; set; }

    public CaptainAccessRule ToRule() => new(Group.CharacterGroupId, Claim.UserId, CanApprove);
}


