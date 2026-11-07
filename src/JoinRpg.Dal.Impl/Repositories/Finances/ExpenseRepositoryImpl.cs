namespace JoinRpg.Dal.Impl.Repositories;

internal class ExpenseRepositoryImpl(MyDbContext ctx) : GameRepositoryImplBase(ctx), IExpenseRepository
{
    public async Task<IReadOnlyCollection<ExpenseInfo>> GetExpenses(ProjectIdentification projectId)
        => await MapExpenses(await Ctx.Set<Expense>()
            .Where(e => e.ProjectId == projectId.Value)
            .ToListAsync(), projectId);

    public async Task<IReadOnlyCollection<ExpenseInfo>> GetExpensesByMaster(ProjectIdentification projectId, UserIdentification masterId)
        => await MapExpenses(await Ctx.Set<Expense>()
            .Where(e => e.ProjectId == projectId.Value && e.MasterId == masterId.Value)
            .ToListAsync(), projectId);

    public async Task<IReadOnlyCollection<ExpenseCategoryInfo>> GetCategories(ProjectIdentification projectId)
        => [.. (await Ctx.Set<ExpenseCategory>()
                .Where(c => c.ProjectId == projectId.Value)
                .ToListAsync())
            .Select(ExpenseMapper.MapCategory)];

    private async Task<IReadOnlyCollection<ExpenseInfo>> MapExpenses(List<Expense> expenses, ProjectIdentification projectId)
    {
        var categoryNames = await Ctx.Set<ExpenseCategory>()
            .Where(c => c.ProjectId == projectId.Value)
            .ToDictionaryAsync(c => c.ExpenseCategoryId, c => c.Name);
        return [.. expenses.Select(e => ExpenseMapper.Map(e, categoryNames))];
    }
}
