using JoinRpg.Services.Impl.Finances;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.Test.Finances;

/// <summary>
/// Юнит-тесты построителя текста ежемесячного финансового отчёта для Telegram. Суммы
/// форматируются через ru-RU ("#,0.##") с нормализацией: типографский минус U+2212 и
/// неразрывные пробелы разделителя тысяч заменяются на ASCII, чтобы вывод был стабилен
/// между Windows и Linux (ICU) и тест предсказуем.
/// </summary>
public class BudgetMonthlyReportMessageBuilderTest
{
    private static readonly BudgetPeriodSums October2026 = new(
        BudgetPeriodKind.Month, new DateOnly(2026, 10, 1), Income: 100000m, Expense: 60000m);

    private static readonly BudgetPeriodSums Year2026ToDate = new(
        BudgetPeriodKind.YearToDate, new DateOnly(2026, 1, 1), Income: 500000m, Expense: 300000m);

    [Fact]
    public void Build_ProfitMonthAndYear_FormatsReport()
    {
        var message = BudgetMonthlyReportMessageBuilder.Build(October2026, Year2026ToDate);

        message.Contents.ShouldBe(
            "День добрый, хотим отчитаться о поступлениях и расходах за Октябрь 2026 года.\n" +
            "Донаты и другие доходы 100 000 рублей, Хостинг и другие расходы 60 000 рублей, итого 40 000 рублей профицит.\n" +
            "За весь 2026 год Донаты и другие доходы 500 000 рублей, Хостинг и другие расходы 300 000 рублей, итого 200 000 рублей профицит.\n" +
            "Подробности можно узнать на <a href=\"https://joinrpg.ru/about/budget\">joinrpg.ru/about/budget</a>\n" +
            "<em>Подпись @joinrpg : принимаем донаты (<a href=\"https://joinrpg.ru/about/donate\">https://joinrpg.ru/about/donate</a>) : отвечаем в поддержке (<a href=\"https://joinrpg.ru/support\">https://joinrpg.ru/support</a>)</em>");
    }

    [Fact]
    public void Build_DeficitMonth_UsesDeficitWordAndAsciiMinus()
    {
        var deficitMonth = October2026 with { Income = 300m, Expense = 500m };

        var message = BudgetMonthlyReportMessageBuilder.Build(deficitMonth, Year2026ToDate);

        message.Contents.ShouldContain("итого -200 рублей дефицит.");
        message.Contents.ShouldContain("итого 200 000 рублей профицит.");
    }
}
