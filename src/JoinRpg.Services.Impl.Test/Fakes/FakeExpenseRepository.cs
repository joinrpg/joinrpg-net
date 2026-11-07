using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Чтение категорий расходов поверх коллекции мока (ADR030). Сервис использует только
/// <see cref="GetCategories"/> — для find-or-create пути категории; остальные методы
/// сознательно не поддержаны.
/// </summary>
internal sealed class FakeExpenseRepository(IReadOnlyCollection<ExpenseCategory> categories) : IExpenseRepository
{
    public Task<IReadOnlyCollection<ExpenseCategoryInfo>> GetCategories(ProjectIdentification projectId)
        => Task.FromResult<IReadOnlyCollection<ExpenseCategoryInfo>>([.. categories
            .Where(c => c.ProjectId == projectId.Value)
            .Select(c => new ExpenseCategoryInfo(
                new ExpenseCategoryIdentification(projectId, c.ExpenseCategoryId),
                c.Name,
                c.ParentCategoryId is { } parentId ? new ExpenseCategoryIdentification(projectId, parentId) : null,
                c.IsActive))]);

    public Task<IReadOnlyCollection<ExpenseInfo>> GetExpenses(ProjectIdentification projectId)
        => throw new NotSupportedException();

    public Task<IReadOnlyCollection<ExpenseInfo>> GetExpensesByMaster(ProjectIdentification projectId, UserIdentification masterId)
        => throw new NotSupportedException();
}
