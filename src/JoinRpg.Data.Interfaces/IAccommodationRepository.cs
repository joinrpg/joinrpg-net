using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces;

public interface IAccommodationRepository
{
    Task<IReadOnlyCollection<ProjectAccommodationType>> GetAccommodationForProject(int projectId);

    Task<IReadOnlyCollection<ClaimAccommodationInfoRow>> GetClaimAccommodationReport(int project);

    Task<IReadOnlyCollection<RoomTypeInfoRow>> GetRoomTypesForProject(int project);

    Task<ProjectAccommodationType> GetRoomTypeById(int roomTypeId);
}

public class RoomTypeInfoRow
{
    public required ProjectAccommodationType RoomType { get; set; }
    public int Occupied { get; set; }
    public int RoomsCount { get; set; }
    public int ApprovedClaims { get; set; }

    /// <summary>
    /// Number of rooms of this type that have nobody living in them
    /// </summary>
    public int FullyFreeRoomsCount { get; set; }

    /// <summary>
    /// Number of rooms of this type that are filled to capacity
    /// </summary>
    public int FullyOccupiedRoomsCount { get; set; }
}

public class ClaimAccommodationInfoRow
{
    public int ClaimId { get; set; }
    public string? AccomodationType { get; set; }
    public string? RoomName { get; set; }
    public required User User { get; set; }
}
