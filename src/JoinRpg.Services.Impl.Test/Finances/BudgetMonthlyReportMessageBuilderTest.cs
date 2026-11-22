using JoinRpg.Services.Impl.Finances;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.Test.Finances;

/// <summary>
/// Юнит-тесты построителя текста ежемесячного финансового отчёта для Telegram. Формат пиннится
/// снапшотами Verify (см. BudgetMonthlyReportMessageBuilderTest.Build_FormatsReport.*.verified.txt).
/// Суммы форматируются через ru-RU ("#,0.##") с нормализацией в типографский минус U+2212 и
/// обычные пробелы в разделителе тысяч — чтобы вывод был стабилен между Windows и Linux (ICU).
/// </summary>
public class BudgetMonthlyReportMessageBuilderTest
{
    private static readonly BudgetPeriodSums October2026 = new(
        BudgetPeriodKind.Month, new DateOnly(2026, 10, 1), Income: 100000m, Expense: 60000m);

    private static readonly BudgetPeriodSums Year2026ToDate = new(
        BudgetPeriodKind.YearToDate, new DateOnly(2026, 1, 1), Income: 500000m, Expense: 300000m);

    [Theory]
    [InlineData(1)] // месяц и год с профицитом
    [InlineData(2)] // месяц с дефицитом — типографский минус U+2212 и слово «дефицит»
    public Task Build_FormatsReport(int num)
    {
        var (month, yearToDate) = num switch
        {
            1 => (October2026, Year2026ToDate),
            2 => (October2026 with { Income = 300m, Expense = 500m }, Year2026ToDate),
            _ => throw new ArgumentOutOfRangeException(nameof(num)),
        };

        return Verify(BudgetMonthlyReportMessageBuilder.Build(month, yearToDate).Contents).UseParameters(num);
    }
}
