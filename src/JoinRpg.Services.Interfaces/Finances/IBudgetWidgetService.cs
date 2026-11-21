namespace JoinRpg.Services.Interfaces;

/// <summary>
/// Сводка бюджета проекта по завершённым периодам для виджета бюджета: доходы (взносы минус
/// возвраты) и одобренные расходы с фактом за 3 завершённых календарных месяца (новый → старый),
/// текущий год без верхней границы и последний завершённый год.
/// </summary>
public interface IBudgetWidgetService
{
    Task<BudgetReport> LoadBudgetReport(ProjectIdentification budgetProject, DateOnly today);
}

/// <summary>Суммы дохода и расхода за один период отчёта виджета бюджета.</summary>
public record BudgetPeriodSums(BudgetPeriodKind Kind, DateOnly Start, decimal Income, decimal Expense);

/// <summary>Сводка бюджета: ровно 5 периодов в том же порядке, что отдаёт калькулятор периодов
/// (3 месяца от нового к старому, YearToDate, завершённый год).</summary>
public record BudgetReport(IReadOnlyCollection<BudgetPeriodSums> Periods);
