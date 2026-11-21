using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.Services.Impl.Finances;

/// <summary>
/// Сервис сводки бюджета по завершённым периодам для виджета бюджета. Операции (взносы
/// и возвраты) читаются одним запросом с Start завершённого года, доход периода — сумма
/// всех операций в нём (возвраты отрицательные и вычитаются). Расход учитывается, только
/// если одобрен, у него есть фактическая сумма и дата, попадающая в период.
/// </summary>
internal class BudgetWidgetService(
    IFinanceOperationsRepository financeOperationsRepository,
    IExpenseRepository expenseRepository) : IBudgetWidgetService
{
    public async Task<BudgetReport> LoadBudgetReport(ProjectIdentification budgetProject, DateOnly today)
    {
        var periods = BudgetPeriods.For(today);
        var from = periods.Single(period => period.Kind == BudgetPeriodKind.CompletedYear).Start;

        var operations = await financeOperationsRepository.GetApprovedMoneyOperations(budgetProject, from);
        var expenses = await expenseRepository.GetExpenses(budgetProject);

        var sums = periods.Select(period =>
        {
            var income = operations
                .Where(operation => InPeriod(period, operation.Date))
                .Sum(operation => (decimal)operation.AmountRubles);
            var expense = expenses
                .Where(e => e.State == ExpenseState.Approved
                    && e.ActualCost is not null
                    && e.SpentAt is { } spentAt
                    && spentAt >= from
                    && InPeriod(period, spentAt))
                .Sum(e => e.ActualCost ?? 0m);
            return new BudgetPeriodSums(period.Kind, period.Start, income, expense);
        }).ToArray();

        return new BudgetReport(sums);
    }

    private static bool InPeriod(BudgetPeriod period, DateOnly date)
        => date >= period.Start && (period.ExclusiveEnd is null || date < period.ExclusiveEnd);
}
