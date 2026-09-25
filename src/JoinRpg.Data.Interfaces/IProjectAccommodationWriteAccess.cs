using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces;

/// <summary>
/// Именованные загрузчики поселения для <see cref="IProjectMetadataUpdateHandle"/>.
/// </summary>
/// <remarks>
/// <para>
/// Типы поселения, комнаты и заявки на поселение относятся к агрегату проекта, но в
/// <see cref="ProjectInfo"/> не входят и навигаций у <see cref="Project"/> для них нет —
/// поэтому их приходится грузить отдельными запросами. Важно, что запросы идут через тот же
/// <c>DbContext</c>, что и хэндл: иначе мутация трекалась бы в одном контексте, а
/// <c>SaveChanges</c> шёл бы в другом (ADR009).
/// </para>
/// <para>
/// Все загрузчики ограничены проектом хэндла — обратиться к чужой комнате через них нельзя.
/// </para>
/// </remarks>
public interface IProjectAccommodationWriteAccess
{
    /// <summary>
    /// Тип поселения этого проекта вместе с комнатами и их жильцами.
    /// <c>null</c>, если типа нет или он принадлежит другому проекту.
    /// </summary>
    Task<ProjectAccommodationType?> LoadRoomType(int roomTypeId);

    /// <summary>
    /// Комната этого проекта вместе с типом поселения и жильцами.
    /// <c>null</c>, если комнаты нет или она принадлежит другому проекту.
    /// </summary>
    Task<ProjectAccommodation?> LoadRoom(int roomId);

    /// <summary>
    /// Заселённые комнаты этого проекта; <paramref name="roomTypeId"/> при необходимости
    /// ограничивает выборку одним типом поселения.
    /// </summary>
    Task<IReadOnlyCollection<ProjectAccommodation>> LoadOccupiedRooms(int? roomTypeId);

    /// <summary>
    /// Заявки на поселение этого проекта по их идентификаторам.
    /// </summary>
    Task<IReadOnlyCollection<AccommodationRequest>> LoadAccommodationRequests(IReadOnlyCollection<int> requestIds);
}
