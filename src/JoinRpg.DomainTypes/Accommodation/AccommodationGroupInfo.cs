using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.DomainTypes.Accommodation;

/// <summary>Группа, живущая (или желающая жить) вместе — одна <c>AccommodationRequest</c></summary>
/// <param name="Id">Идентификатор заявки на проживание</param>
/// <param name="AccommodationTypeId">
/// Тип проживания, по которому куплена группа. Сегодня у всех групп плана он одинаков и выводится
/// из плана, после разделения типа и категории (ADR018) станет существенным.
/// </param>
/// <param name="RoomId">Комната, в которой живёт группа, или <c>null</c>, если ещё не расселена</param>
/// <param name="Subjects">Заявки игроков, составляющих группу</param>
public record class AccommodationGroupInfo(
    AccommodationRequestIdentification Id,
    AccommodationTypeIdentification AccommodationTypeId,
    AccommodationRoomIdentification? RoomId,
    IReadOnlyCollection<ClaimIdentification> Subjects)
{
    /// <summary>Сколько человек в группе</summary>
    public int Persons => Subjects.Count;
}
