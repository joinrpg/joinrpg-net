namespace JoinRpg.Blazor.Client.CommonPages;

/// <summary>Модель виджета бюджета: завершённые месяцы и годы.</summary>
public record BudgetWidgetModel(BudgetPeriodCard[] Months, BudgetPeriodCard[] Years);

/// <summary>Карточка одного периода бюджета.</summary>
public record BudgetPeriodCard(string Title, decimal Income, decimal Expense)
{
    /// <summary>Профицит (положительный) или дефицит (отрицательный) периода.</summary>
    public decimal Total => Income - Expense;
}
