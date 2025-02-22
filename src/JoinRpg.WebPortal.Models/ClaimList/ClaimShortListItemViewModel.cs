using System.ComponentModel.DataAnnotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimShortListItemViewModel(string name, int claimId, ProjectIdentification projectId, UserLinkViewModel player)
{
    public ClaimShortListItemViewModel(ClaimWithPlayer claimWithPlayer)
        : this(claimWithPlayer.CharacterName, claimWithPlayer.ClaimId, new(claimWithPlayer.ProjectId), UserLinks.Create(claimWithPlayer.Player))
    {

    }

    [Obsolete]
    public ClaimShortListItemViewModel(Claim claim)
        : this(claim.Character.CharacterName, claim.ClaimId, new(claim.ProjectId), UserLinks.Create(claim.Player))
    {

    }
    [Display(Name = "Заявка")]
    public string Name { get; } = name;

    public int ClaimId { get; } = claimId;
    public ProjectIdentification ProjectId { get; } = projectId;

    public UserLinkViewModel PlayerLink { get; } = player;
}
