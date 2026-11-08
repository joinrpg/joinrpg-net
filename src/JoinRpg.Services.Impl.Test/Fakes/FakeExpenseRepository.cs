using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Чтение категорий расходов поверх коллекции мока (ADR030). Сервис использует
/// <see cref="GetCategories"/> — для find-or-create пути категории и подсказок путей
/// (флаг HasActiveDirectExpenses считается по привязанным расходам); методы расходов
/// сознательно не поддержаны.
/// </summary>
internal sealed class FakeExpenseRepository(
    IReadOnlyCollection<ExpenseCategory> categories,
    IReadOnlyCollection<Expense>? expenses = null) : IExpenseRepository
{
    public Task<IReadOnlyCollection<ExpenseCategoryInfo>> GetCategories(ProjectIdentification projectId)
    {
        var projectCategories = categories.Where(c => c.ProjectId == projectId.Value).ToArray();
        var names = projectCategories.ToDictionary(c => c.ExpenseCategoryId, c => c.Name);
        var usedIds = (expenses ?? [])
            .Where(e => e.ProjectId == projectId.Value && e.State != ExpenseState.Rejected)
            .Select(e => e.CategoryId)
            .ToHashSet();
        return Task.FromResult<IReadOnlyCollection<ExpenseCategoryInfo>>([.. projectCategories
            .Select(c => new ExpenseCategoryInfo(
                new ExpenseCategoryIdentification(projectId, c.ExpenseCategoryId),
                c.Name,
                c.ParentCategoryId is { } parentId ? new ExpenseCategoryIdentification(projectId, parentId) : null,
                c.IsActive,
                usedIds.Contains(c.ExpenseCategoryId),
                FullPath: string.Join("\\", c.ParentCategoryIds.Select(id => names[id]).Append(c.Name))))]);
    }

    public Task<IReadOnlyCollection<ExpenseInfo>> GetExpenses(ProjectIdentification projectId)
        => throw new NotSupportedException();

    public Task<IReadOnlyCollection<ExpenseInfo>> GetExpensesByMaster(ProjectIdentification projectId, UserIdentification masterId)
        => throw new NotSupportedException();
}
