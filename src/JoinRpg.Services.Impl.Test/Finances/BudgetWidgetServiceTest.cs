using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.Services.Impl.Finances;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.Test.Finances;

/// <summary>
/// Юнит-тесты сервиса сводки бюджета по завершённым периодам (виджет бюджета). ТДД: написаны
/// против контракта <see cref="IBudgetWidgetService"/>, реализация <c>BudgetWidgetService</c>
/// появится позже. При <c>today = 2026-11-21</c> периоды: октябрь/сентябрь/август 2026 (новый →
/// старый), YearToDate с 2026-01-01 без верхней границы, завершённый 2025 год. Операция попадает
/// в период, если Start &lt;= Date &lt; ExclusiveEnd; доход — сумма всех операций периода
/// (возвраты отрицательные), расход — только одобренный с фактом и датой в периоде.
/// </summary>
public class BudgetWidgetServiceTest
{
    private static readonly DateOnly Today = new(2026, 11, 21);

    private readonly ProjectIdentification projectId = new(4);
    private readonly List<Expense> expenses = [];
    private readonly List<ApprovedMoneyOperation> operations = [];
    private readonly FakeMoneyOperationsRepository moneyOperationsRepository;

    public BudgetWidgetServiceTest()
    {
        moneyOperationsRepository = new FakeMoneyOperationsRepository(operations);
    }

    private BudgetWidgetService CreateService()
        => new(moneyOperationsRepository, new FakeExpenseRepository([], expenses));

    [Fact]
    public async Task LoadBudgetReport_DistributesIncomeAndExpensesAcrossPeriods()
    {
        operations.Add(new ApprovedMoneyOperation(new DateOnly(2026, 9, 15), 5000));
        expenses.Add(CreateExpense(ExpenseState.Approved, actualQuantity: 1, actualPrice: 3000m, spentAt: new DateOnly(2026, 10, 20)));

        var report = await CreateService().LoadBudgetReport(projectId, Today);

        report.Periods.ShouldBe([
            Month(2026, 10, Income: 0, Expense: 3000),
            Month(2026, 9, Income: 5000, Expense: 0),
            Month(2026, 8, Income: 0, Expense: 0),
            YearToDate(Income: 5000, Expense: 3000),
            CompletedYear(Income: 0, Expense: 0),
        ]);
    }

    [Fact]
    public async Task LoadBudgetReport_RefundInOctoberDecreasesOctoberIncome()
    {
        operations.Add(new ApprovedMoneyOperation(new DateOnly(2026, 10, 10), 5000));
        operations.Add(new ApprovedMoneyOperation(new DateOnly(2026, 10, 25), -2000));

        var report = await CreateService().LoadBudgetReport(projectId, Today);

        report.Periods.ShouldBe([
            Month(2026, 10, Income: 3000, Expense: 0),
            Month(2026, 9, Income: 0, Expense: 0),
            Month(2026, 8, Income: 0, Expense: 0),
            YearToDate(Income: 3000, Expense: 0),
            CompletedYear(Income: 0, Expense: 0),
        ]);
    }

    [Fact]
    public async Task LoadBudgetReport_SkipsExpensesWithoutFactApprovalOrDate()
    {
        // Одобрен без факта — ActualCost null
        expenses.Add(CreateExpense(ExpenseState.Approved, spentAt: new DateOnly(2026, 10, 20)));
        // Не одобрен (Proposed), хотя факт и дата есть
        expenses.Add(CreateExpense(ExpenseState.Proposed, actualQuantity: 1, actualPrice: 4000m, spentAt: new DateOnly(2026, 10, 21)));
        // Одобрен с фактом, но без даты
        expenses.Add(CreateExpense(ExpenseState.Approved, actualQuantity: 1, actualPrice: 2000m));

        var report = await CreateService().LoadBudgetReport(projectId, Today);

        report.Periods.ShouldBe([
            Month(2026, 10, Income: 0, Expense: 0),
            Month(2026, 9, Income: 0, Expense: 0),
            Month(2026, 8, Income: 0, Expense: 0),
            YearToDate(Income: 0, Expense: 0),
            CompletedYear(Income: 0, Expense: 0),
        ]);
    }

    [Fact]
    public async Task LoadBudgetReport_OperationOnMonthStartGoesToNewMonth()
    {
        // 01.09 — граница: операция попадает в сентябрь, а не в август
        operations.Add(new ApprovedMoneyOperation(new DateOnly(2026, 9, 1), 7000));
        operations.Add(new ApprovedMoneyOperation(new DateOnly(2026, 8, 31), 1000));

        var report = await CreateService().LoadBudgetReport(projectId, Today);

        report.Periods.ShouldBe([
            Month(2026, 10, Income: 0, Expense: 0),
            Month(2026, 9, Income: 7000, Expense: 0),
            Month(2026, 8, Income: 1000, Expense: 0),
            YearToDate(Income: 8000, Expense: 0),
            CompletedYear(Income: 0, Expense: 0),
        ]);
    }

    [Fact]
    public async Task LoadBudgetReport_ReadsOperationsFromCompletedYearStart()
    {
        operations.Add(new ApprovedMoneyOperation(new DateOnly(2025, 6, 1), 12000));
        // Раньше Start завершённого года: репозиторий отфильтрует, в отчёте не учитывается
        operations.Add(new ApprovedMoneyOperation(new DateOnly(2024, 12, 31), 99999));
        expenses.Add(CreateExpense(ExpenseState.Approved, actualQuantity: 1, actualPrice: 5000m, spentAt: new DateOnly(2025, 7, 15)));

        var report = await CreateService().LoadBudgetReport(projectId, Today);

        // Операции читаются с Start завершённого года — 2025-01-01
        moneyOperationsRepository.RequestedFrom.ShouldBe(new DateOnly(2025, 1, 1));
        report.Periods.ShouldBe([
            Month(2026, 10, Income: 0, Expense: 0),
            Month(2026, 9, Income: 0, Expense: 0),
            Month(2026, 8, Income: 0, Expense: 0),
            YearToDate(Income: 0, Expense: 0),
            CompletedYear(Income: 12000, Expense: 5000),
        ]);
    }

    [Fact]
    public async Task LoadBudgetReport_EmptyData_AllPeriodsZero()
    {
        var report = await CreateService().LoadBudgetReport(projectId, Today);

        report.Periods.Count.ShouldBe(5);
        report.Periods.ShouldBe([
            Month(2026, 10, Income: 0, Expense: 0),
            Month(2026, 9, Income: 0, Expense: 0),
            Month(2026, 8, Income: 0, Expense: 0),
            YearToDate(Income: 0, Expense: 0),
            CompletedYear(Income: 0, Expense: 0),
        ]);
    }

    private Expense CreateExpense(
        ExpenseState state,
        int? actualQuantity = null,
        decimal? actualPrice = null,
        DateOnly? spentAt = null)
        => new()
        {
            ProjectId = projectId.Value,
            Name = "Аренда домика",
            MasterId = 1,
            CreatedById = 1,
            PlannedQuantity = 1,
            PlannedPrice = 5000m,
            ActualQuantity = actualQuantity,
            ActualPrice = actualPrice,
            SpentAt = spentAt?.ToDateTime(TimeOnly.MinValue),
            State = state,
        };

    private static BudgetPeriodSums Month(int year, int month, decimal Income, decimal Expense)
        => new(BudgetPeriodKind.Month, new DateOnly(year, month, 1), Income, Expense);

    private static BudgetPeriodSums YearToDate(decimal Income, decimal Expense)
        => new(BudgetPeriodKind.YearToDate, new DateOnly(2026, 1, 1), Income, Expense);

    private static BudgetPeriodSums CompletedYear(decimal Income, decimal Expense)
        => new(BudgetPeriodKind.CompletedYear, new DateOnly(2025, 1, 1), Income, Expense);
}
