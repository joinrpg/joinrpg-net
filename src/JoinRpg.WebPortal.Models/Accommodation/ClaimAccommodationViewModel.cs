using JoinRpg.DataModel;
using JoinRpg.Domain;


namespace JoinRpg.Web.Models.Accommodation;

/// <summary>
/// Панель «Проживание» на странице заявки. Приглашения и выбор типа проживания живут
/// в островах <c>JoinRpg.Web.Accommodation</c> и данных отсюда не берут.
/// </summary>
public class ClaimAccommodationViewModel
{
    public ClaimAccommodationViewModel(Claim claim)
    {
        AccommodationRequest = claim.AccommodationRequest;
        ClaimId = claim.ClaimId;
        ProjectId = claim.ProjectId;
        Neighbours = claim.GetClaimNeighbours();

        RoomFreeSpace = claim.AccommodationRequest?.GetRoomFreeSpace() ?? 0;
    }

    public int ClaimId { get; }
    public int ProjectId { get; }

    public ClaimIdentification ClaimIdentification => new(ProjectId, ClaimId);
    public AccommodationRequest? AccommodationRequest { get; }

    public int RoomFreeSpace { get; }

    public IReadOnlyCollection<User> Neighbours { get; }
}
