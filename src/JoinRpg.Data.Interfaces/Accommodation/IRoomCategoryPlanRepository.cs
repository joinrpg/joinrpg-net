using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Data.Interfaces.Accommodation;

/// <summary>
/// Загрузка доменного агрегата поселения — плана категории комнат (ADR018).
/// </summary>
/// <remarks>
/// Отдельный интерфейс, а не расширение <see cref="IAccommodationRepository"/>: тот отдаёт
/// EF-сущности и плоские строки отчётов. Здесь каждый метод делает ровно один запрос с явной
/// проекцией, без <c>Include</c> и без прогрева контекста всем проектом (см. ADR011).
/// Межзапросного кеша у плана нет: он привязан к конкретному экземпляру <c>ProjectInfo</c>.
/// </remarks>
public interface IRoomCategoryPlanRepository
{
    /// <summary>
    /// План пула, из которого селится данный тип проживания. Пока тип и категория не разделены,
    /// это план одноимённой категории; после разделения один план будут возвращать несколько типов.
    /// </summary>
    Task<RoomCategoryPlan?> GetPlanForTypeOrDefault(AccommodationTypeIdentification typeId);

    /// <summary>
    /// Планы всех категорий проекта. Все они разделяют один экземпляр <c>ProjectInfo</c>.
    /// </summary>
    /// <remarks>
    /// Для экранов, которым нужно расселение сразу многих заявок: печать конвертов открывается на
    /// весь проект, и спрашивать план по типу каждой заявки означало бы запрос на тип. Выборка одна
    /// и та же по форме, что у <see cref="GetPlanForTypeOrDefault"/>, только без фильтра по категории.
    /// </remarks>
    Task<IReadOnlyCollection<RoomCategoryPlan>> GetAllPlans(ProjectIdentification projectId);
}
