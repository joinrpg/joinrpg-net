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
    /// Селит группы жильцов в комнату. Группа обязана принадлежать тому же пулу, что и комната.
    /// </summary>
    /// <param name="roomId">Комната, в которую селим.</param>
    /// <param name="groupIds">Группы жильцов (заявки на проживание), которые въезжают.</param>
    Task OccupyRoom(
        AccommodationRoomIdentification roomId,
        IReadOnlyCollection<AccommodationRequestIdentification> groupIds);

    /// <summary>
    /// Выселяет одну группу жильцов из её комнаты.
    /// </summary>
    Task UnOccupyGroup(AccommodationRequestIdentification groupId);

    /// <summary>
    /// Выселяет из комнаты всех её жильцов.
    /// </summary>
    Task UnOccupyRoom(AccommodationRoomIdentification roomId);

    /// <summary>
    /// Выселить все группы данного типа проживания. Соседи из других типов той же категории
    /// остаются в комнатах (ADR020).
    /// </summary>
    /// <remarks>Атомарна: одна загрузка плана, один проход, одно сохранение (ADR018, §1).</remarks>
    Task UnOccupyRoomType(AccommodationTypeIdentification typeId);

    /// <summary>
    /// Выселяет всех жильцов всех комнат проекта.
    /// </summary>
    /// <remarks>
    /// Остаётся циклом по категориям — по транзакции на каждую (ADR018, §1). Частичное выполнение
    /// не страшно: выселение идемпотентно, повторный запуск дочищает остаток.
    /// </remarks>
    Task UnOccupyAllRooms(ProjectIdentification projectId);
}

