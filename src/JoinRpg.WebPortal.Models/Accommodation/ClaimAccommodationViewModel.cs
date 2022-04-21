using JoinRpg.DataModel;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Models.Accommodation;

public class ClaimAccommodationViewModel
{
    public int ClaimId { get; set; }
    public int ProjectId { get; set; }
    public IEnumerable<ProjectAccommodationType> AvailableAccommodationTypes { get; set; }
    public IEnumerable<AccommodationPotentialNeighbors> PotentialNeighbors { get; set; }
    public IEnumerable<AccommodationInvite> IncomingInvite { get; set; }
    public IEnumerable<AccommodationInvite> OutgoingInvite { get; set; }
    public CharacterNavigationViewModel Navigation;
    public AccommodationRequest AccommodationRequest { get; set; }
    public bool AccommodationEnabledForClaim { get; set; }
}
