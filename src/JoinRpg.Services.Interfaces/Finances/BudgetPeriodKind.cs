namespace JoinRpg.Services.Interfaces;

/// <summary>
/// Вид периода агрегации в отчёте виджета бюджета (см. <c>BudgetPeriodSums</c>).
/// </summary>
public enum BudgetPeriodKind
{
    Month,
    YearToDate,
    CompletedYear,
}
