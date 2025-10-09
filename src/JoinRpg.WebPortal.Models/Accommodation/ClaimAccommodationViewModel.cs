using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;


namespace JoinRpg.Web.Models.Accommodation;

public class ClaimAccommodationViewModel
{
    public ClaimAccommodationViewModel(
        IEnumerable<ProjectAccommodationType> availableAccommodationTypes,
        IEnumerable<AccommodationPotentialNeighbors> potentialNeighbors,
        IEnumerable<AccommodationInvite> incomingInvite,
        IEnumerable<AccommodationInvite> outgoingInvite,
        Claim claim,
        ICurrentUserAccessor currentUser
        )
    {
        var hasMasterAccess = claim.HasMasterAccess(currentUser);
        AvailableAccommodationTypes = availableAccommodationTypes.Where(a => a.IsPlayerSelectable || a.Id == claim.AccommodationRequest?.AccommodationTypeId || hasMasterAccess).ToList();
        PotentialNeighbors = potentialNeighbors;
        AccommodationRequest = claim.AccommodationRequest;
        IncomingInvite = incomingInvite;
        OutgoingInvite = outgoingInvite;
        ClaimId = claim.ClaimId;
        ProjectId = claim.ProjectId;
        Neighbours = claim.GetClaimNeighbours();

        RoomFreeSpace = claim.AccommodationRequest?.GetRoomFreeSpace() ?? 0;
    }



    public int ClaimId { get; }
    public int ProjectId { get; }
    public IEnumerable<ProjectAccommodationType> AvailableAccommodationTypes { get; }
    public IEnumerable<AccommodationPotentialNeighbors> PotentialNeighbors { get; }
    public IEnumerable<AccommodationInvite> IncomingInvite { get; }
    public IEnumerable<AccommodationInvite> OutgoingInvite { get; }
    public AccommodationRequest? AccommodationRequest { get; }

    public int RoomFreeSpace { get; }

    public IReadOnlyCollection<User> Neighbours { get; }
}
