namespace JoinRpg.Services.Interfaces;

public interface IAccommodationService
{
    /// <summary>
    /// Добавляет комнаты в пул (категорию комнат), а не в тип проживания (ADR018).
    /// </summary>
    /// <param name="categoryId">Категория комнат, в которую добавляются комнаты.</param>
    /// <param name="roomNames">
    /// Готовые имена комнат. Разбор пользовательского ввода («1,2,5-8» и прочий синтаксис формы)
    /// сюда не попадает — он остаётся в web-слое (<c>RoomNamesParser</c>).
    /// </param>
    /// <returns>Идентификаторы созданных комнат.</returns>
    Task<IReadOnlyCollection<AccommodationRoomIdentification>> AddRooms(
        RoomCategoryIdentification categoryId,
        IReadOnlyCollection<string> roomNames);

    /// <summary>
    /// Переименовывает комнату.
    /// </summary>
    Task RenameRoom(AccommodationRoomIdentification roomId, string name);

    /// <summary>
    /// Удаляет комнату. Удалить можно только незаселённую комнату.
    /// </summary>
    Task DeleteRoom(AccommodationRoomIdentification roomId);

    /// <summary>
    /// Move inhabitants to room
    /// </summary>
    Task OccupyRoom(OccupyRequest request);

    /// <summary>
    /// Move specific inhabitant coupling (aka AgreementRequest) from romm
    /// </summary>
    Task UnOccupyRoom(UnOccupyRequest request);

    /// <summary>
    /// Remove all inhabitants from room
    /// </summary>
    Task UnOccupyRoomAll(UnOccupyAllRequest request);

    /// <summary>
    /// Removes all inhabitants from all rooms of the specified type
    /// </summary>
    Task UnOccupyRoomType(int projectId, int roomTypeId);

    /// <summary>
    /// Removes all inhabitants from all rooms of all types
    /// </summary>
    Task UnOccupyAll(int projectId);
}

public class OccupyRequest
{
    public int ProjectId { get; set; }
    public int RoomId { get; set; }
    public required IReadOnlyCollection<int> AccommodationRequestIds { get; set; }
}

public class UnOccupyRequest
{
    public int ProjectId { get; set; }
    public int AccommodationRequestId { get; set; }
}

public class UnOccupyAllRequest
{
    public int ProjectId { get; set; }
    public int RoomId { get; set; }
}

