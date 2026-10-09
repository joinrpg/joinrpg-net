using System.Text.Json.Serialization;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.Web.Accommodation.Rooms;

/// <summary>
/// Группа проживающих — те, кто живёт (или хочет жить) вместе, одна заявка на проживание.
/// </summary>
/// <param name="GroupId">Заявка на проживание</param>
/// <param name="TypeId">
/// Тип проживания, по которому куплена группа. Из одной категории комнат могут селиться несколько
/// типов (ADR020), и «Выселить всех» на странице типа выселяет только его группы.
/// </param>
/// <param name="TypeCapacity">
/// Вместимость типа проживания, по которому куплена группа: она ограничивает всю комнату, куда
/// группа въедет (ADR018, «Правило свободного места»).
/// </param>
/// <param name="Residents">Жильцы группы — их заявки: персонаж и игрок</param>
/// <param name="FeeTotal">Сколько всего должна группа за участие</param>
/// <param name="FeeToPay">Сколько группе осталось доплатить; переплата сюда не попадает — тогда ноль</param>
public record AccommodationGroupViewModel(
    AccommodationRequestIdentification GroupId,
    AccommodationTypeIdentification TypeId,
    int TypeCapacity,
    IReadOnlyList<ClaimLinkViewModel> Residents,
    int FeeTotal,
    int FeeToPay)
{
    /// <summary>Сколько мест группа занимает</summary>
    [JsonIgnore]
    public int Persons => Residents.Count;

    /// <summary>Имена жильцов через запятую</summary>
    [JsonIgnore]
    public string PersonsList => string.Join(", ", Residents.Select(r => r.PlayerName.DisplayName));
}

/// <summary>
/// Комната с жильцами.
/// </summary>
/// <param name="RoomId">Комната</param>
/// <param name="Name">Название (номер) комнаты</param>
/// <param name="Groups">Группы, расселённые в эту комнату</param>
public record AccommodationRoomViewModel(
    AccommodationRoomIdentification RoomId,
    string Name,
    IReadOnlyList<AccommodationGroupViewModel> Groups);

/// <summary>
/// Всё, что нужно контролу расселения на странице «Комнаты» типа проживания.
/// </summary>
/// <param name="TypeId">Тип проживания, чья это страница</param>
/// <param name="ProjectName">Название проекта — для заголовка страницы</param>
/// <param name="TypeName">Название типа проживания — для заголовка страницы</param>
/// <param name="TypeCapacity">Сколько человек селится в комнату по этому типу</param>
/// <param name="RoomCapacity">Физическая вместимость комнаты пула (ADR018)</param>
/// <param name="Rooms">Комнаты пула</param>
/// <param name="UnassignedGroups">Группы, ещё не расселённые по комнатам</param>
/// <param name="CanManageRooms">Право заводить, переименовывать и удалять комнаты</param>
/// <param name="CanAssignRooms">Право расселять и выселять игроков</param>
public record RoomTypeRoomsViewModel(
    AccommodationTypeIdentification TypeId,
    string ProjectName,
    string TypeName,
    int TypeCapacity,
    int RoomCapacity,
    IReadOnlyList<AccommodationRoomViewModel> Rooms,
    IReadOnlyList<AccommodationGroupViewModel> UnassignedGroups,
    bool CanManageRooms,
    bool CanAssignRooms);

/// <summary>
/// Сервер отказал в операции по понятной пользователю причине — например, в комнате не хватает
/// мест. Причина уже сформулирована для показа.
/// </summary>
public class AccommodationRoomsOperationRefusedException(string message) : Exception(message);

/// <summary>
/// Управление комнатами и расселение на странице «Комнаты» типа проживания.
/// </summary>
/// <remarks>
/// Права проверяет сервис расселения: управление комнатами — <c>CanManageAccommodation</c>,
/// расселение — <c>CanSetPlayersAccommodations</c>.
/// </remarks>
public interface IAccommodationRoomsClient
{
    /// <summary>Комнаты и группы пула, из которого селится тип проживания</summary>
    Task<RoomTypeRoomsViewModel> GetRooms(AccommodationTypeIdentification typeId);

    /// <summary>
    /// Добавить комнаты по списку из поля ввода («1, 2, 5-8»). Возвращает обновлённое состояние:
    /// идентификаторы новых комнат знает только сервер.
    /// </summary>
    Task<RoomTypeRoomsViewModel> AddRooms(AccommodationTypeIdentification typeId, string roomNames);

    /// <summary>Переименовать комнату</summary>
    Task RenameRoom(AccommodationRoomIdentification roomId, string name);

    /// <summary>Удалить комнату. Удалить можно только пустую</summary>
    Task DeleteRoom(AccommodationRoomIdentification roomId);

    /// <summary>Заселить группы в комнату. Не влезли все — не въезжает никто</summary>
    Task OccupyRoom(
        AccommodationRoomIdentification roomId,
        IReadOnlyCollection<AccommodationRequestIdentification> groupIds);

    /// <summary>Выселить одну группу</summary>
    Task UnOccupyGroup(AccommodationRequestIdentification groupId);

    /// <summary>Освободить комнату — выселить всех её жильцов</summary>
    Task UnOccupyRoom(AccommodationRoomIdentification roomId);

    /// <summary>Выселить всех, кто живёт по этому типу проживания</summary>
    Task UnOccupyRoomType(AccommodationTypeIdentification typeId);
}
