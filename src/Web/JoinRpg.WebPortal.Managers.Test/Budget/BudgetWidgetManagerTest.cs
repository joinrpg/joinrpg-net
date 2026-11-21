using JoinRpg.DomainTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.WebPortal.Managers.Budget;

namespace JoinRpg.WebPortal.Managers.Test.Budget;

/// <summary>
/// Юнит-тесты менеджера сборки модели виджета бюджета (страница /about). Заголовки на русском:
/// месяц — «Октябрь 2026», YearToDate — «С начала 2026», CompletedYear — «2025». Годы в модели:
/// сначала YearToDate, потом CompletedYear. Месяцы — в том порядке, что в отчёте.
/// </summary>
public class BudgetWidgetManagerTest
{
    private static readonly ProjectIdentification BudgetProject = new(1);

    private static readonly BudgetPeriodSums October = new(BudgetPeriodKind.Month, new DateOnly(2026, 10, 1), Income: 100m, Expense: 40m);
    private static readonly BudgetPeriodSums September = new(BudgetPeriodKind.Month, new DateOnly(2026, 9, 1), Income: 80m, Expense: 60m);
    private static readonly BudgetPeriodSums YearToDate = new(BudgetPeriodKind.YearToDate, new DateOnly(2026, 1, 1), Income: 500m, Expense: 200m);
    private static readonly BudgetPeriodSums CompletedYear = new(BudgetPeriodKind.CompletedYear, new DateOnly(2025, 1, 1), Income: 1000m, Expense: 700m);

    private static BudgetWidgetManager CreateManager(BudgetReport report) => new(new StubBudgetWidgetService(report));

    [Fact]
    public async Task LoadWidgetModel_BuildsLocalizedTitles()
    {
        var model = await CreateManager(new BudgetReport([October, September, YearToDate, CompletedYear]))
            .LoadWidgetModel(BudgetProject);

        model.Months.Select(m => m.Title).ShouldBe(["Октябрь 2026", "Сентябрь 2026"]);
        model.Years.Select(y => y.Title).ShouldBe(["С начала 2026", "2025"]);
    }

    [Fact]
    public async Task LoadWidgetModel_OrdersYearsYearToDateFirst()
    {
        var model = await CreateManager(new BudgetReport([CompletedYear, October, YearToDate, September]))
            .LoadWidgetModel(BudgetProject);

        model.Months.Select(m => m.Title).ShouldBe(["Октябрь 2026", "Сентябрь 2026"]);
        model.Years.Select(y => y.Income).ShouldBe([YearToDate.Income, CompletedYear.Income]);
    }

    [Fact]
    public async Task LoadWidgetModel_KeepsSumsAndComputesTotal()
    {
        var model = await CreateManager(new BudgetReport([October, September, YearToDate, CompletedYear]))
            .LoadWidgetModel(BudgetProject);

        var october = model.Months[0];
        october.Income.ShouldBe(100m);
        october.Expense.ShouldBe(40m);
        october.Total.ShouldBe(60m);

        var completedYear = model.Years[1];
        completedYear.Total.ShouldBe(300m);
    }

    private sealed class StubBudgetWidgetService(BudgetReport report) : IBudgetWidgetService
    {
        public Task<BudgetReport> LoadBudgetReport(ProjectIdentification budgetProject, DateOnly today)
            => Task.FromResult(report);
    }
}
