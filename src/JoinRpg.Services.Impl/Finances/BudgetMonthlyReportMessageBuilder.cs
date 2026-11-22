using System.Globalization;

namespace JoinRpg.Services.Impl.Finances;

/// <summary>
/// Построитель текста ежемесячного финансового отчёта для Telegram. Разметка — Telegram HTML:
/// только &lt;strong&gt;/&lt;em&gt;/&lt;a&gt; переживают санитайзер (см. TelegramSenderJobService).
/// </summary>
internal static class BudgetMonthlyReportMessageBuilder
{
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static TelegramHtmlString Build(BudgetPeriodSums month, BudgetPeriodSums yearToDate)
    {
        var monthTitle = month.Start.ToString("MMMM yyyy", RussianCulture);
        var year = yearToDate.Start.Year;

        var text =
            "День добрый, с вами ежемесячный отчёт о бюджете JoinRpg.\n" +
            $"За {monthTitle}: донаты и другие доходы {FormatMoney(month.Income)}₽, хостинг и другие расходы {FormatMoney(month.Expense)}₽, итого {FormatMoney(Total(month))}₽ {ProfitWord(Total(month))}.\n" +
            $"За весь {year} год: донаты и другие доходы {FormatMoney(yearToDate.Income)}₽, хостинг и другие расходы {FormatMoney(yearToDate.Expense)}₽, итого {FormatMoney(Total(yearToDate))}₽ {ProfitWord(Total(yearToDate))}.\n" +
            "Подробности можно узнать на <a href=\"https://joinrpg.ru/about/budget\">joinrpg.ru/about/budget</a>\n" +
            "<em>Подпись @joinrpg : принимаем донаты (<a href=\"https://joinrpg.ru/about/donate\">https://joinrpg.ru/about/donate</a>) : отвечаем в поддержке (<a href=\"https://joinrpg.ru/support\">https://joinrpg.ru/support</a>)</em>";

        return new TelegramHtmlString(text);
    }

    // Разделитель тысяч из ICU (на Linux CI это неразрывные пробелы) нормализуем в обычные,
    // а минус — в типографский U+2212: в сообщении канала он лучше смотрится (по образцу
    // BudgetWidget.razor, только нормализация минуса в другую сторону).
    private static string FormatMoney(decimal value) => value
        .ToString("#,0.##", RussianCulture)
        .Replace('-', '−')
        .Replace('\u00A0', ' ')
        .Replace(' ', ' ');

    private static string ProfitWord(decimal total) => total >= 0 ? "профицит" : "дефицит";

    private static decimal Total(BudgetPeriodSums period) => period.Income - period.Expense;
}
