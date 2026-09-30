using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Data.Interfaces.Accommodation;

/// <summary>
/// Репозиторий для изменения агрегата поселения — плана категории комнат (ADR018). Гарантирует
/// согласованность трекаемых EF-сущностей (<see cref="ProjectAccommodation"/>,
/// <see cref="AccommodationRequest"/>) и доменного снимка <see cref="RoomCategoryPlan"/>.
/// </summary>
/// <remarks>
/// Берётся <b>только</b> из <c>IUnitOfWork</c>, а не из DI: <c>MyDbContext</c> зарегистрирован
/// транзиентом, поэтому DI-экземпляр получил бы другой <c>DbContext</c> — мутация трекалась бы
/// в одном, а <c>SaveChanges</c> шёл бы в другом (ADR009 §1, ADR014 §2).
/// </remarks>
public interface IRoomCategoryPlanWriteRepository
{
    /// <summary>
    /// Загружает план категории комнат: трекаемые сущности вместе с согласованным доменным снимком.
    /// </summary>
    /// <param name="projectInfo">
    /// Снимок метаданных проекта. Приходит снаружи, а не собирается здесь: репозиторий его только
    /// читает, а вызывающий берёт его из кешированного на запрос <c>IProjectMetadataRepository</c>.
    /// Конструктор агрегата требует, чтобы это был ровно тот же экземпляр (ADR018, §3).
    /// </param>
    /// <param name="categoryId">Категория комнат, план которой будет изменён.</param>
    /// <param name="tracking">Что грузить трекаемым помимо категории и её комнат.</param>
    /// <exception cref="JoinRpgEntityNotFoundException">Категории комнат в проекте нет.</exception>
    Task<IRoomCategoryPlanUpdateHandle> LoadPlanForUpdate(
        ProjectInfo projectInfo,
        RoomCategoryIdentification categoryId,
        RoomCategoryPlanTracking tracking);

    /// <summary>
    /// То же, но корень агрегата назван через комнату: пул в идентификаторе комнаты не закодирован,
    /// поэтому план ищется по принадлежности комнаты категории (ADR018, §10).
    /// </summary>
    /// <param name="projectInfo">См. <see cref="LoadPlanForUpdate"/>.</param>
    /// <param name="projectInfo">См. <see cref="LoadPlanForUpdate"/>.</param>
    /// <param name="roomId">Комната, которая будет изменена.</param>
    /// <param name="tracking">Что грузить трекаемым помимо категории и её комнат.</param>
    /// <exception cref="AccommodationRoomNotFoundException">
    /// Комнаты с таким идентификатором в этом проекте нет. Именно эта проверка закрывает дефект 2
    /// ADR018: чужую комнату не отредактировать и не удалить, даже зная её номер.
    /// </exception>
    Task<IRoomCategoryPlanUpdateHandle> LoadPlanForRoomUpdate(
        ProjectInfo projectInfo,
        AccommodationRoomIdentification roomId,
        RoomCategoryPlanTracking tracking);

    /// <summary>
    /// То же, но корень агрегата назван через группу жильцов: пул в идентификаторе группы не
    /// закодирован, поэтому план ищется по принадлежности группы категории (ADR018, §10).
    /// </summary>
    /// <param name="projectInfo">См. <see cref="LoadPlanForUpdate"/>.</param>
    /// <param name="groupId">Группа жильцов, которая будет изменена.</param>
    /// <remarks>
    /// Параметра <c>tracking</c> здесь нет: назвать корень агрегата через группу может только
    /// операция, которая эту группу и двигает, поэтому группы грузятся всегда.
    /// </remarks>
    /// <exception cref="AccommodationGroupNotFoundException">
    /// Группы с таким идентификатором в этом проекте нет. Фильтр по проекту здесь так же
    /// обязателен, как у комнаты.
    /// </exception>
    Task<IRoomCategoryPlanUpdateHandle> LoadPlanForGroupUpdate(
        ProjectInfo projectInfo,
        AccommodationRequestIdentification groupId);
}

/// <summary>
/// Что операции нужно в трекаемом виде помимо ряда категории и её комнат (ADR018, §10).
/// </summary>
/// <remarks>
/// Управление комнатами (создание, переименование, удаление) групп жильцов не меняет — кто где
/// живёт, ему видно по доменному снимку <see cref="RoomCategoryPlan"/>. Поэтому запрос за
/// трекаемыми группами делается не всегда, а только там, где они реально мутируются.
/// </remarks>
public enum RoomCategoryPlanTracking
{
    /// <summary>Только ряд категории и её комнаты: два запроса на мутацию.</summary>
    RoomsOnly,

    /// <summary>
    /// Плюс трекаемые группы жильцов пула, включая нерасселённые: третий запрос. Нужен
    /// заселению и выселению (PR 5).
    /// </summary>
    WithGroups,
}

/// <summary>
/// Согласованная пара: трекаемые EF-сущности комнат и групп плюс доменный снимок
/// <see cref="RoomCategoryPlan"/> строго ДО мутации (ADR018, §10).
/// </summary>
/// <remarks>
/// Хэндл не отдаёт <c>DbSet&lt;T&gt;</c> наружу — только <see cref="Add"/> и <see cref="Remove"/>.
/// Это и не даёт вызывающему уйти в другой <c>DbContext</c>, и позволяет подделать хэндл
/// в юнит-тестах, не поднимая EF (как в ADR014).
/// </remarks>
public interface IRoomCategoryPlanUpdateHandle
{
    /// <summary>Снимок метаданных проекта, на который ссылается <see cref="Plan"/>.</summary>
    ProjectInfo ProjectInfo { get; }

    /// <summary>
    /// Доменный снимок плана строго <b>ДО</b> изменения. После мутации не обновляется: кеша
    /// у плана нет (ADR018, §3), значит нечему и устаревать.
    /// </summary>
    RoomCategoryPlan Plan { get; }

    /// <summary>
    /// Трекаемый ряд категории комнат. Своей таблицы у категории пока нет, поэтому её ряд —
    /// это <see cref="ProjectAccommodationType"/> (ADR018, «Задел на разделение», пункт 1).
    /// Нужен созданию комнат: новая комната принадлежит категории.
    /// </summary>
    ProjectAccommodationType Category { get; }

    /// <summary>Трекаемые комнаты пула по их идентификаторам.</summary>
    IReadOnlyDictionary<AccommodationRoomIdentification, ProjectAccommodation> Rooms { get; }

    /// <summary>Трекаемые группы жильцов пула по их идентификаторам, включая нерасселённые.</summary>
    /// <remarks>
    /// Заполнены, только если операция попросила <see cref="RoomCategoryPlanTracking.WithGroups"/>:
    /// управлению комнатами группы не нужны, и лишнего запроса за ними не делается. Обращение без
    /// запроса — ошибка программиста, а не данных, поэтому сразу <see cref="InvalidOperationException"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Операция не запрашивала трекаемые группы.</exception>
    IReadOnlyDictionary<AccommodationRequestIdentification, AccommodationRequest> Groups { get; }

    /// <summary>
    /// Добавляет новую сущность в тот же <c>DbContext</c>, через который потом идёт
    /// <c>SaveChanges</c>.
    /// </summary>
    void Add(object entity);

    /// <summary>
    /// Окончательно удаляет сущность из того же <c>DbContext</c>, через который потом идёт
    /// <c>SaveChanges</c>.
    /// </summary>
    void Remove(object entity);
}
