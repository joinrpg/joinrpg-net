using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models.ClaimList;

[Obsolete("Use ClaimLinkViewModel")]
public class ClaimShortListItemViewModel(string name, ClaimIdentification claimId, UserLinkViewModel player)
{
    [Display(Name = "Заявка")]
    public string Name { get; } = name;

    public int ClaimId { get; } = claimId.ClaimId;
    public ProjectIdentification ProjectId { get; } = claimId.ProjectId;

    public UserLinkViewModel PlayerLink { get; } = player;
}
