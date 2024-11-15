using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimShortListItemViewModel(Claim claim)
{
    [Display(Name = "Заявка")]
    public string Name { get; } = claim.Character.CharacterName;

    public int ClaimId { get; } = claim.ClaimId;
    public int ProjectId { get; } = claim.ProjectId;

    public UserLinkViewModel PlayerLink { get; } = UserLinks.Create(claim.Player);
}
