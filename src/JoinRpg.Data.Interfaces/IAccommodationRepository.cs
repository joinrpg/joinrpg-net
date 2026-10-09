using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Data.Interfaces;

public interface IAccommodationRepository
{
    /// <summary>
    /// Расселён ли хоть кто-то из купивших этот тип проживания.
    /// </summary>
    Task<bool> HasOccupiedRoomOfType(AccommodationTypeIdentification accommodationTypeId);

    /// <summary>
    /// Заявки, состоящие в группах проживания (<c>AccommodationRequest</c>) этого типа, — и
    /// расселённых, и нет.
    /// </summary>
    /// <remarks>
    /// Нужно удалению типа: перед ним группы расформировываются, иначе база каскадом удалила бы
    /// группы, на которые ещё ссылаются заявки.
    /// </remarks>
    Task<IReadOnlyCollection<ClaimIdentification>> GetClaimsInGroupsOfType(
        AccommodationTypeIdentification accommodationTypeId);

    Task<IReadOnlyCollection<ClaimAccommodationInfoRow>> GetClaimAccommodationReport(int project);
}

/// <summary>
/// Строка отчёта по расселению.
/// </summary>
/// <remarks>
/// Игрок представлен отображаемым именем и телефоном, а не сущностью <c>User</c> (ADR013):
/// сущность в DTO означала бы, что к любой её навигации можно обратиться выше по стеку, и
/// отчёт снова начал бы догружать данные по строке.
/// </remarks>
public class ClaimAccommodationInfoRow
{
    public int ClaimId { get; set; }
    public string? AccomodationType { get; set; }
    public string? RoomName { get; set; }
    public required UserDisplayName PlayerName { get; set; }
    public PhoneNumber? Phone { get; set; }
}
