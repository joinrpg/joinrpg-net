using System.ComponentModel.DataAnnotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimShortListItemViewModel(string name, ClaimIdentification claimId, UserLinkViewModel player)
{
    public ClaimShortListItemViewModel(ClaimWithPlayer claimWithPlayer)
        : this(claimWithPlayer.CharacterName, claimWithPlayer.ClaimId, UserLinks.Create(claimWithPlayer.Player))
    {

    }

    [Obsolete]
    public ClaimShortListItemViewModel(Claim claim)
        : this(claim.Character.CharacterName, claim.GetId(), UserLinks.Create(claim.Player))
    {

    }
    [Display(Name = "Заявка")]
    public string Name { get; } = name;

    public int ClaimId { get; } = claimId.ClaimId;
    public ProjectIdentification ProjectId { get; } = claimId.ProjectId;

    public UserLinkViewModel PlayerLink { get; } = player;
}
