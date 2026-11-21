namespace JoinRpg.Dal.Impl.Repositories;

using LinqKit;

internal class ExpenseRepositoryImpl(MyDbContext ctx) : GameRepositoryImplBase(ctx), IExpenseRepository
{
    public Task<bool> HasActualExpensesSince(ProjectIdentification projectId, DateTime cutoffUtc)
        => Ctx.Set<Expense>().AnyAsync(e =>
            e.ProjectId == projectId.Value
            && ExpensePredicates.ActualSpendingSince(cutoffUtc).Invoke(e));

    public async Task<IReadOnlyCollection<ExpenseInfo>> GetExpenses(ProjectIdentification projectId)
        => await MapExpenses(await Ctx.Set<Expense>()
            .Where(e => e.ProjectId == projectId.Value)
            .OrderByDescending(e => e.UpdatedAt)
            .ThenByDescending(e => e.ExpenseId)
            .ToListAsync(), projectId);

    public async Task<IReadOnlyCollection<ExpenseInfo>> GetExpensesByMaster(ProjectIdentification projectId, UserIdentification masterId)
        => await MapExpenses(await Ctx.Set<Expense>()
            .Where(e => e.ProjectId == projectId.Value && e.MasterId == masterId.Value)
            .OrderByDescending(e => e.UpdatedAt)
            .ThenByDescending(e => e.ExpenseId)
            .ToListAsync(), projectId);

    public async Task<IReadOnlyCollection<ExpenseCategoryInfo>> GetCategories(ProjectIdentification projectId)
    {
        var rows = await Ctx.Set<ExpenseCategory>()
            .Where(c => c.ProjectId == projectId.Value)
            .Select(c => new
            {
                Category = c,
                // EXISTS-подзапрос: у категории есть привязанный расход не в состоянии Rejected
                HasActiveDirectExpenses = Ctx.Set<Expense>().Any(e =>
                    e.ProjectId == c.ProjectId
                    && e.CategoryId == c.ExpenseCategoryId
                    && e.State != ExpenseState.Rejected),
            })
            .ToListAsync();
        var categoryNames = rows.ToDictionary(r => r.Category.ExpenseCategoryId, r => r.Category.Name);
        return [.. rows.Select(r => ExpenseMapper.MapCategory(r.Category, r.HasActiveDirectExpenses, categoryNames))];
    }

    private async Task<IReadOnlyCollection<ExpenseInfo>> MapExpenses(List<Expense> expenses, ProjectIdentification projectId)
    {
        var categoryNames = await Ctx.Set<ExpenseCategory>()
            .Where(c => c.ProjectId == projectId.Value)
            .ToDictionaryAsync(c => c.ExpenseCategoryId, c => c.Name);
        return [.. expenses.Select(e => ExpenseMapper.Map(e, categoryNames))];
    }
}
