using System.Globalization;
using JoinRpg.Blazor.Client.CommonPages;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.WebPortal.Managers.Budget;

/// <summary>Менеджер сборки модели виджета бюджета для страницы /about.</summary>
public sealed class BudgetWidgetManager(IBudgetWidgetService budgetWidgetService)
{
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    public async Task<BudgetWidgetModel> LoadWidgetModel(ProjectIdentification budgetProject)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var report = await budgetWidgetService.LoadBudgetReport(budgetProject, today);

        var months = report.Periods
            .Where(period => period.Kind == BudgetPeriodKind.Month)
            .Select(ToMonthCard)
            .ToArray();
        var years = report.Periods
            .Where(period => period.Kind is BudgetPeriodKind.YearToDate or BudgetPeriodKind.CompletedYear)
            .OrderBy(period => period.Kind == BudgetPeriodKind.CompletedYear ? 1 : 0)
            .Select(ToYearCard)
            .ToArray();

        return new BudgetWidgetModel(months, years);
    }

    private static BudgetPeriodCard ToMonthCard(BudgetPeriodSums period)
    {
        var title = period.Start.ToString("MMMM yyyy", RussianCulture);
        return new BudgetPeriodCard(CapitalizeFirstLetter(title), period.Income, period.Expense);
    }

    private static BudgetPeriodCard ToYearCard(BudgetPeriodSums period) => period.Kind switch
    {
        BudgetPeriodKind.YearToDate => new BudgetPeriodCard($"С начала {period.Start.Year}", period.Income, period.Expense),
        BudgetPeriodKind.CompletedYear => new BudgetPeriodCard(period.Start.Year.ToString(), period.Income, period.Expense),
        _ => throw new ArgumentOutOfRangeException(nameof(period)),
    };

    private static string CapitalizeFirstLetter(string title)
        => title.Length == 0 ? title : char.ToUpperInvariant(title[0]) + title[1..];
}
