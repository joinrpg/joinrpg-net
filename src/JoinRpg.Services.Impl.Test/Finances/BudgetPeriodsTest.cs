using JoinRpg.Services.Impl.Finances;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.Test.Finances;

/// <summary>
/// Юнит-тесты калькулятора завершённых периодов бюджета (виджет бюджета). ТДД: написаны
/// против контракта <c>BudgetPeriods</c>, реализация появится позже. Месяц закрывается
/// с 16-го числа следующего, год — с 31 января следующего.
/// </summary>
public class BudgetPeriodsTest
{
    [Fact]
    public void For_RegularDate_ReturnsThreeMonthsYearToDateAndCompletedYear()
    {
        var periods = BudgetPeriods.For(new DateOnly(2026, 11, 21));

        periods.ShouldBe([
            Month(2026, 10),
            Month(2026, 9),
            Month(2026, 8),
            new BudgetPeriod(BudgetPeriodKind.YearToDate, new DateOnly(2026, 1, 1), null),
            new BudgetPeriod(BudgetPeriodKind.CompletedYear, new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1)),
        ]);
    }

    [Theory]
    // 2026-11-15: октябрь ещё не закрыт (закрывается 16 ноября) — первый месяц сентябрь
    [InlineData("2026-11-15", 2026, 9)]
    // 2026-11-16: октябрь закрыт — первый месяц октябрь
    [InlineData("2026-11-16", 2026, 10)]
    public void For_MonthBoundary_ClosesOn16thOfNextMonth(string today, int year, int firstMonth)
    {
        var periods = BudgetPeriods.For(DateOnly.Parse(today));

        periods.ShouldBe([
            Month(year, firstMonth),
            Month(year, firstMonth - 1),
            Month(year, firstMonth - 2),
            new BudgetPeriod(BudgetPeriodKind.YearToDate, new DateOnly(2026, 1, 1), null),
            new BudgetPeriod(BudgetPeriodKind.CompletedYear, new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1)),
        ]);
    }

    [Theory]
    // 2027-01-30: год 2026 ещё не закрыт (закрывается 31 января) — CompletedYear 2025, YTD 2026
    [InlineData("2027-01-30", 2026, 2025)]
    // 2027-01-31: год 2026 закрыт — CompletedYear 2026, YTD 2027
    [InlineData("2027-01-31", 2027, 2026)]
    public void For_YearBoundary_ClosesOn31stOfJanuary(string today, int yearToDateYear, int completedYear)
    {
        var periods = BudgetPeriods.For(DateOnly.Parse(today));

        periods.ShouldBe([
            Month(2026, 12),
            Month(2026, 11),
            Month(2026, 10),
            new BudgetPeriod(BudgetPeriodKind.YearToDate, new DateOnly(yearToDateYear, 1, 1), null),
            new BudgetPeriod(BudgetPeriodKind.CompletedYear, new DateOnly(completedYear, 1, 1), new DateOnly(completedYear + 1, 1, 1)),
        ]);
    }

    [Fact]
    public void For_January_ReturnsNonOverlappingPeriodsOfPreviousYear()
    {
        var periods = BudgetPeriods.For(new DateOnly(2027, 1, 10));

        // Декабрь ещё не закрыт (закрывается 16 января) — месяцы ноябрь/октябрь/сентябрь 2026
        periods.ShouldBe([
            Month(2026, 11),
            Month(2026, 10),
            Month(2026, 9),
            new BudgetPeriod(BudgetPeriodKind.YearToDate, new DateOnly(2026, 1, 1), null),
            new BudgetPeriod(BudgetPeriodKind.CompletedYear, new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1)),
        ]);
    }

    private static BudgetPeriod Month(int year, int month)
        => new(BudgetPeriodKind.Month, new DateOnly(year, month, 1), new DateOnly(year, month, 1).AddMonths(1));
}
