namespace JoinRpg.Data.Interfaces;

/// <summary>
/// Чтение расходов проекта (ADR030). Возвращает доменные типы, не DataModel —
/// преобразование в internal-маппере в Dal (паттерн RoomCategoryPlanMapper, ADR018).
/// </summary>
public interface IExpenseRepository
{
    Task<IReadOnlyCollection<ExpenseInfo>> GetExpenses(ProjectIdentification projectId);

    Task<IReadOnlyCollection<ExpenseInfo>> GetExpensesByMaster(ProjectIdentification projectId, UserIdentification masterId);

    Task<IReadOnlyCollection<ExpenseCategoryInfo>> GetCategories(ProjectIdentification projectId);

    /// <summary>Была ли фактическая трата (одобренный расход с заполненным фактом) не раньше <paramref name="cutoffUtc"/>.</summary>
    Task<bool> HasActualExpensesSince(ProjectIdentification projectId, DateTime cutoffUtc);
}
