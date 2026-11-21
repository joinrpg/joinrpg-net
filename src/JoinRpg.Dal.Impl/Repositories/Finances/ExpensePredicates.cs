namespace JoinRpg.Dal.Impl.Repositories;

internal static class ExpensePredicates
{
    /// <summary>
    /// Фактическая трата, понесённая не раньше <paramref name="cutoffUtc"/>:
    /// одобренный расход с заполненным фактом и датой трат внутри окна.
    /// </summary>
    /// <remarks>
    /// Единственный источник условия «фактическая трата за окно» в запросах
    /// (например, <c>ExpenseRepositoryImpl.HasActualExpensesSince</c>). За соответствием
    /// условия следит тест-страж <c>ExpensePredicatesTest</c>: он перебирает все значения
    /// <c>ExpenseState</c> и граничные случаи по факту и дате.
    /// </remarks>
    public static Expression<Func<Expense, bool>> ActualSpendingSince(DateTime cutoffUtc)
        => e => e.State == ExpenseState.Approved && e.ActualQuantity != null && e.SpentAt >= cutoffUtc;
}
