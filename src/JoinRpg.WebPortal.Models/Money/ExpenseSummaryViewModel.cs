using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.Web.Models;

public record ExpenseSummaryRow(
    [property: Display(Name = "Категория")] string CategoryPath,
    [property: Display(Name = "План (одобрено)")] decimal ApprovedPlannedCost,
    [property: Display(Name = "Факт (одобрено)")] decimal ApprovedActualCost,
    [property: Display(Name = "На рассмотрении")] decimal ProposedCost);

/// <summary>
/// Блок расходов в финансовой сводке проекта (ADR030): суммы по категориям в разрезе
/// состояний согласования. Отклонённые расходы ни в одну сумму не входят.
/// </summary>
public class ExpenseSummaryViewModel
{
    public IReadOnlyCollection<ExpenseSummaryRow> Rows { get; }

    [Display(Name = "Потрачено (одобрено)")]
    public decimal TotalSpent { get; }

    internal ExpenseSummaryViewModel(IReadOnlyCollection<ExpenseSummaryRow> rows, decimal totalSpent)
    {
        Rows = rows;
        TotalSpent = totalSpent;
    }
}

public static class ExpenseSummaryBuilder
{
    public static ExpenseSummaryViewModel Build(IReadOnlyCollection<ExpenseInfo> expenses)
    {
        ArgumentNullException.ThrowIfNull(expenses);

        // Строка выводится, только если в категорию что-то входит: отклонённые расходы
        // не дают ни одной суммы (эффекты состояний — в доменном типе ExpenseInfo), поэтому
        // категории только из них отсекаются общим фильтром строк, где все суммы нулевые.
        var rows = expenses
            .GroupBy(CategoryPathOf)
            .Select(g => new ExpenseSummaryRow(
                g.Key,
                ApprovedPlannedCost: g.Where(e => e.State == ExpenseState.Approved).Sum(e => e.PlannedCost),
                ApprovedActualCost: g.Sum(e => e.ActualBalanceEffect),
                ProposedCost: g.Where(e => e.State == ExpenseState.Proposed).Sum(e => e.PlannedBalanceEffect)))
            .Where(r => r.ApprovedPlannedCost != 0 || r.ApprovedActualCost != 0 || r.ProposedCost != 0)
            .OrderBy(r => r.CategoryPath)
            .ToArray();

        var totalSpent = expenses.Sum(e => e.ActualBalanceEffect);

        return new ExpenseSummaryViewModel(rows, totalSpent);
    }

    private static string CategoryPathOf(ExpenseInfo expense)
        => string.Join("\\", expense.CategoryPath.Select(c => c.Name));
}
