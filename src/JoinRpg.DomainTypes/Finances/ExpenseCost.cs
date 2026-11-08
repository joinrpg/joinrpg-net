namespace JoinRpg.DomainTypes.Finances;

/// <summary>
/// Арифметика стоимости расхода (ADR030): план и факт — количество × цена.
/// Общая для <see cref="ExpenseInfo"/> и кода, работающего с сущностью расхода напрямую.
/// </summary>
public static class ExpenseCost
{
    /// <summary>Плановая стоимость: количество × цена.</summary>
    public static int Planned(int quantity, int price) => quantity * price;

    /// <summary>Фактическая стоимость, если факт заполнен, иначе null.</summary>
    public static int? Actual(int? quantity, int? price)
        => quantity is { } q && price is { } p ? q * p : null;
}
