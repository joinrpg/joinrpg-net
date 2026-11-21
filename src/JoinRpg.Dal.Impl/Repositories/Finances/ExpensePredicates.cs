namespace JoinRpg.Dal.Impl.Repositories;

internal static class ExpensePredicates
{
    /// <summary>
    /// Фактическая трата с датой в полуинтервале [<paramref name="fromUtc"/>, <paramref name="toUtc"/>):
    /// одобренный расход с заполненным фактом и датой трат внутри периода.
    /// </summary>
    /// <remarks>
    /// Единственный источник условия «фактическая трата за период» в запросах
    /// (например, <c>ExpenseRepositoryImpl.HasActualExpensesInPeriod</c>). За соответствием
    /// условия следит тест-страж <c>ExpensePredicatesTest</c>: он перебирает все значения
    /// <c>ExpenseState</c> и граничные случаи по факту и обеим границам периода.
    /// </remarks>
    public static Expression<Func<Expense, bool>> ActualSpendingInPeriod(DateTime fromUtc, DateTime toUtc)
        => e => e.State == ExpenseState.Approved
            && e.ActualQuantity != null
            && e.SpentAt >= fromUtc
            && e.SpentAt < toUtc;
}
