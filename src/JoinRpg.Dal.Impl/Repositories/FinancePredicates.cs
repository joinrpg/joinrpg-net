namespace JoinRpg.Dal.Impl.Repositories;

internal static class FinancePredicates
{
    /// <summary>
    /// Операции, ждущие решения мастера.
    /// </summary>
    /// <remarks>
    /// Это намеренная копия правила из вычисляемого свойства
    /// <see cref="FinanceOperation.RequireModeration"/>: то свойство — обычный C#-геттер, и EF6
    /// не умеет перевести его в SQL, поэтому в проекциях нужно именно дерево выражений. Чтобы
    /// копия не разошлась с оригиналом, за ней следит тест-страж
    /// <c>FinancePredicatesTest</c>: он сравнивает скомпилированный предикат со свойством на всех
    /// комбинациях <c>FinanceOperationState</c> и <c>FinanceOperationType</c>.
    /// </remarks>
    public static Expression<Func<FinanceOperation, bool>> RequireModeration()
        => fo => fo.State == FinanceOperationState.Proposed
            && (fo.OperationType == FinanceOperationType.Submit
                || fo.OperationType == FinanceOperationType.PreferentialFeeRequest);
}
