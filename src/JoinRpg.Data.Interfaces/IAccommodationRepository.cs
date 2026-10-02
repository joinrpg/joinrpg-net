using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Data.Interfaces;

public interface IAccommodationRepository
{
    /// <summary>
    /// Есть ли у этого типа проживания хоть одна комната, в которой кто-то живёт.
    /// </summary>
    Task<bool> HasOccupiedRoomOfType(AccommodationTypeIdentification accommodationTypeId);

    Task<IReadOnlyCollection<ClaimAccommodationInfoRow>> GetClaimAccommodationReport(int project);

    Task<IReadOnlyCollection<RoomTypeInfoRow>> GetRoomTypesForProject(ProjectIdentification projectId);
}

/// <summary>
/// Сводка занятости по одному типу проживания для страницы «Поселение».
/// </summary>
/// <remarks>
/// Сам тип проживания — настройка проекта: он лежит в <c>ProjectInfo.AccommodationSettings</c>
/// (ADR015) и здесь представлен только идентификатором. Экземпляр <c>AccommodationTypeInfo</c>
/// репозиторий отдавать не должен: единственный законный экземпляр принадлежит <c>ProjectInfo</c>
/// (инвариант ссылочного равенства, ADR013), а DAL пришлось бы либо смастерить свой — и инвариант
/// сломать, — либо принимать <c>ProjectInfo</c> параметром только затем, чтобы вернуть вызывающему
/// то, что у него уже есть. Счётчики, наоборот, — оперативные данные, и считает их SQL.
/// </remarks>
public class RoomTypeInfoRow
{
    public required AccommodationTypeIdentification RoomTypeId { get; set; }
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
