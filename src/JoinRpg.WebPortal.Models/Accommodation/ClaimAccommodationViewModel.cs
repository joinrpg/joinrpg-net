using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Accommodation;

public class ClaimAccommodationViewModel
{
    public int ClaimId { get; set; }
    public int ProjectId { get; set; }
    public required IEnumerable<ProjectAccommodationType> AvailableAccommodationTypes { get; set; }
    public required IEnumerable<AccommodationPotentialNeighbors> PotentialNeighbors { get; set; }
    public required IEnumerable<AccommodationInvite> IncomingInvite { get; set; }
    public required IEnumerable<AccommodationInvite> OutgoingInvite { get; set; }
    public AccommodationRequest? AccommodationRequest { get; set; }
    public bool AccommodationEnabledForClaim { get; set; }
}
