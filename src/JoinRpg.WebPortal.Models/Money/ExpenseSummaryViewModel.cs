using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.Web.Models;

public record ExpenseSummaryRow(
    [property: Display(Name = "Категория")] string CategoryPath,
    [property: Display(Name = "План (одобрено)")] decimal ApprovedPlannedCost,
    [property: Display(Name = "Факт (одобрено)")] decimal ApprovedActualCost,
    [property: Display(Name = "На рассмотрении")] decimal ProposedCost,
    bool IsTotal = false);

/// <summary>
/// Блок расходов в финансовой сводке проекта (ADR030): суммы по категориям в разрезе
/// состояний согласования. Отклонённые расходы ни в одну сумму не входят. Последняя
/// строка — «Итого»: суммы посчитаны по расходам напрямую, чтобы агрегация подкатегорий
/// в родителей не давала двойного счёта.
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

        // Строка выводится, только если в категорию что-то входит: суммы категории включают
        // весь подкатегорный поддерево, поэтому «входит» — в любую подкатегорию. Отклонённые
        // расходы не дают ни одной суммы (эффекты состояний — в доменном типе ExpenseInfo),
        // поэтому категории только из них отсекаются общим фильтром строк, где все суммы нулевые.
        var sums = new Dictionary<string, (decimal Planned, decimal Actual, decimal Proposed)>();
        foreach (var expense in expenses)
        {
            var names = expense.CategoryPath.Select(c => c.Name).ToArray();
            for (var length = 1; length <= names.Length; length++)
            {
                var key = string.Join("\\", names.Take(length));
                var sum = sums.TryGetValue(key, out var existing) ? existing : default;
                sum.Actual += expense.ActualBalanceEffect;
                if (expense.State == ExpenseState.Approved)
                {
                    sum.Planned += expense.PlannedCost;
                }

                if (expense.State == ExpenseState.Proposed)
                {
                    sum.Proposed += expense.PlannedBalanceEffect;
                }

                sums[key] = sum;
            }
        }

        var rows = sums
            .Select(s => new ExpenseSummaryRow(
                s.Key,
                ApprovedPlannedCost: s.Value.Planned,
                ApprovedActualCost: s.Value.Actual,
                ProposedCost: s.Value.Proposed))
            .Where(r => r.ApprovedPlannedCost != 0 || r.ApprovedActualCost != 0 || r.ProposedCost != 0)
            .OrderBy(r => r.CategoryPath)
            .ToArray();

        var totalSpent = expenses.Sum(e => e.ActualBalanceEffect);

        // «Итого» считаем по расходам напрямую, а не суммируя строки: суммы строк включают
        // поддерево, и сумма строк дала бы двойной счёт.
        var totalRow = new ExpenseSummaryRow(
            "Итого",
            ApprovedPlannedCost: expenses.Where(e => e.State == ExpenseState.Approved).Sum(e => e.PlannedCost),
            ApprovedActualCost: totalSpent,
            ProposedCost: expenses.Where(e => e.State == ExpenseState.Proposed).Sum(e => e.PlannedBalanceEffect),
            IsTotal: true);

        return new ExpenseSummaryViewModel([.. rows, totalRow], totalSpent);
    }
}
