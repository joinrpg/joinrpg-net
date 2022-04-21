using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimShortListItemViewModel
{
    [Display(Name = "Заявка")]
    public string Name { get; }

    [Display(Name = "Игрок")]
    public User Player { get; }

    public int ClaimId { get; }
    public int ProjectId { get; }
    public bool IsApproved { get; }

    public ClaimShortListItemViewModel(Claim claim)
    {
        ClaimId = claim.ClaimId;
        Name = claim.Name;
        Player = claim.Player;
        ProjectId = claim.ProjectId;
        IsApproved = claim.IsApproved;
    }
}
