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
    /// <exception cref="JoinRpgEntityNotFoundException">Категории комнат в проекте нет.</exception>
    Task<IRoomCategoryPlanUpdateHandle> LoadPlanForUpdate(
        ProjectInfo projectInfo,
        RoomCategoryIdentification categoryId);

    /// <summary>
    /// То же, но корень агрегата назван через комнату: пул в идентификаторе комнаты не закодирован,
    /// поэтому план ищется по принадлежности комнаты категории (ADR018, §10).
    /// </summary>
    /// <param name="projectInfo">См. <see cref="LoadPlanForUpdate"/>.</param>
    /// <param name="roomId">Комната, которая будет изменена.</param>
    /// <exception cref="AccommodationRoomNotFoundException">
    /// Комнаты с таким идентификатором в этом проекте нет. Именно эта проверка закрывает дефект 2
    /// ADR018: чужую комнату не отредактировать и не удалить, даже зная её номер.
    /// </exception>
    Task<IRoomCategoryPlanUpdateHandle> LoadPlanForRoomUpdate(
        ProjectInfo projectInfo,
        AccommodationRoomIdentification roomId);
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
    /// <remarks>
    /// Трекаемых групп жильцов (<see cref="AccommodationRequest"/>) здесь намеренно нет: ни одна
    /// операция управления комнатами их не меняет, а кто где живёт, видно по доменному снимку
    /// <see cref="Plan"/>. Заселение переедет сюда в PR 5 — тогда и появятся, вместе со своим
    /// потребителем.
    /// </remarks>
    IReadOnlyDictionary<AccommodationRoomIdentification, ProjectAccommodation> Rooms { get; }

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
