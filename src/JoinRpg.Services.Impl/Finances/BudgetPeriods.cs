using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.Finances;

internal readonly record struct BudgetPeriod(BudgetPeriodKind Kind, DateOnly Start, DateOnly? ExclusiveEnd);

/// <summary>
/// Калькулятор завершённых периодов для виджета бюджета: по <paramref name="today"/>
/// возвращает 3 завершённых календарных месяца (от нового к старому), текущий год
/// без верхней границы (<see cref="BudgetPeriodKind.YearToDate"/>) и последний
/// завершённый год. Месяц завершён с 16-го числа следующего, год — с 31 января следующего.
/// </summary>
internal static class BudgetPeriods
{
    public static IReadOnlyList<BudgetPeriod> For(DateOnly today)
    {
        var periods = new List<BudgetPeriod>(capacity: 5);

        var lastCompletedMonth = StartOfMonth(today).AddMonths(-1);
        while (!IsCompletedMonth(lastCompletedMonth, today))
        {
            lastCompletedMonth = lastCompletedMonth.AddMonths(-1);
        }
        for (var i = 0; i < 3; i++)
        {
            var month = lastCompletedMonth.AddMonths(-i);
            periods.Add(new BudgetPeriod(BudgetPeriodKind.Month, month, month.AddMonths(1)));
        }

        var completedYear = today.Year - 1;
        while (!IsCompletedYear(completedYear, today))
        {
            completedYear--;
        }
        periods.Add(new BudgetPeriod(BudgetPeriodKind.YearToDate, new DateOnly(completedYear + 1, 1, 1), null));
        periods.Add(new BudgetPeriod(BudgetPeriodKind.CompletedYear, new DateOnly(completedYear, 1, 1), new DateOnly(completedYear + 1, 1, 1)));

        return periods;
    }

    private static DateOnly StartOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    private static bool IsCompletedMonth(DateOnly monthStart, DateOnly today)
        => today >= monthStart.AddMonths(1).AddDays(15);

    private static bool IsCompletedYear(int year, DateOnly today)
        => today >= new DateOnly(year + 1, 1, 1).AddDays(30);
}
