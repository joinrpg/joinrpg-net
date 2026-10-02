using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Accommodation;

namespace JoinRpg.Services.Impl.Accommodation;

/// <summary>
/// Контекст изменения плана категории комнат (ADR018, §10): доменный снимок ДО мутации,
/// трекаемые EF-сущности комнат (и групп жильцов, если операция их запросила) и разрешённые
/// действия над контекстом БД.
/// Негенерик — чтобы приватные хелперы сервисов принимали его без параметра типа
/// (как <c>ProjectMutationContext</c>, ADR009).
/// </summary>
/// <param name="Plan">Доменный снимок плана строго ДО изменения.</param>
/// <param name="Handle">Хэндл агрегата; наружу из контекста не торчит.</param>
/// <param name="Now">Время выполнения операции.</param>
/// <param name="CurrentUser">Текущий пользователь.</param>
internal abstract record RoomCategoryPlanMutationContext(
    RoomCategoryPlan Plan,
    IRoomCategoryPlanUpdateHandle Handle,
    DateTimeOffset Now,
    ICurrentUserAccessor CurrentUser)
{
    /// <summary>Снимок метаданных проекта — тот же экземпляр, на который ссылается <see cref="Plan"/>.</summary>
    public ProjectInfo ProjectInfo => Handle.ProjectInfo;

    /// <summary>
    /// Уведомления о составе жителей комнаты, поставленные в очередь мутацией. Отправляются
    /// props-сервисом после успешного сохранения, в порядке добавления.
    /// </summary>
    internal List<RoomOccupancyNotification> RoomNotifications { get; } = [];

    /// <summary>
    /// Ставит уведомление о комнате в очередь. Отправится после сохранения.
    /// </summary>
    public void AddRoomNotification(RoomOccupancyNotification notification)
        => RoomNotifications.Add(notification);

    /// <summary>
    /// Трекаемый ряд категории комнат: сегодня это ряд типа проживания (ADR018, «Задел на
    /// разделение», пункт 1). Нужен созданию комнат.
    /// </summary>
    public ProjectAccommodationType Category => Handle.Category;

    /// <summary>Добавляет сущность в тот же <c>DbContext</c>, через который идёт сохранение.</summary>
    public void AddEntity(object entity) => Handle.Add(entity);

    /// <summary>Окончательно удаляет сущность из того же <c>DbContext</c>.</summary>
    public void RemoveEntity(object entity) => Handle.Remove(entity);

    /// <summary>
    /// Трекаемая комната пула, которую можно мутировать.
    /// </summary>
    /// <exception cref="AccommodationRoomNotFoundException">Комнаты с таким идентификатором в этом пуле нет.</exception>
    public ProjectAccommodation GetRoomForChange(AccommodationRoomIdentification roomId)
        => Handle.Rooms.TryGetValue(roomId, out var room)
            ? room
            : throw new AccommodationRoomNotFoundException(roomId);

    /// <summary>
    /// Трекаемая группа жильцов пула, которую можно мутировать. Доступна только операциям,
    /// попросившим <see cref="RoomCategoryPlanTracking.WithGroups"/>, — иначе за группами просто
    /// не ходили в базу.
    /// </summary>
    /// <exception cref="AccommodationGroupNotFoundException">Группы с таким идентификатором в этом пуле нет.</exception>
    public AccommodationRequest GetGroupForChange(AccommodationRequestIdentification groupId)
        => Handle.Groups.TryGetValue(groupId, out var group)
            ? group
            : throw new AccommodationGroupNotFoundException(groupId);
}

/// <summary>
/// Контекст изменения плана с типизированными аргументами операции (<see cref="Request"/>).
/// </summary>
internal sealed record RoomCategoryPlanMutationContext<TArgs>(
    RoomCategoryPlan Plan,
    IRoomCategoryPlanUpdateHandle Handle,
    DateTimeOffset Now,
    ICurrentUserAccessor CurrentUser,
    TArgs Request)
    : RoomCategoryPlanMutationContext(Plan, Handle, Now, CurrentUser);
