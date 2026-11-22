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
        var monthTitle = CapitalizeFirstLetter(month.Start.ToString("MMMM yyyy", RussianCulture));
        var year = yearToDate.Start.Year;

        var text =
            $"День добрый, хотим отчитаться о поступлениях и расходах за {monthTitle} года.\n" +
            $"Донаты и другие доходы {FormatMoney(month.Income)} рублей, Хостинг и другие расходы {FormatMoney(month.Expense)} рублей, итого {FormatMoney(Total(month))} рублей {ProfitWord(Total(month))}.\n" +
            $"За весь {year} год Донаты и другие доходы {FormatMoney(yearToDate.Income)} рублей, Хостинг и другие расходы {FormatMoney(yearToDate.Expense)} рублей, итого {FormatMoney(Total(yearToDate))} рублей {ProfitWord(Total(yearToDate))}.\n" +
            "Подробности можно узнать на <a href=\"https://joinrpg.ru/about/budget\">joinrpg.ru/about/budget</a>\n" +
            "<em>Подпись @joinrpg : принимаем донаты (<a href=\"https://joinrpg.ru/about/donate\">https://joinrpg.ru/about/donate</a>) : отвечаем в поддержке (<a href=\"https://joinrpg.ru/support\">https://joinrpg.ru/support</a>)</em>";

        return new TelegramHtmlString(text);
    }

    // ICU на Linux (CI) даёт типографский минус U+2212 и неразрывные пробелы в разделителе
    // тысяч — нормализуем в ASCII, чтобы вывод был стабилен (по образцу BudgetWidget.razor).
    private static string FormatMoney(decimal value) => value
        .ToString("#,0.##", RussianCulture)
        .Replace('−', '-')
        .Replace('\u00A0', ' ')
        .Replace(' ', ' ');

    private static string ProfitWord(decimal total) => total >= 0 ? "профицит" : "дефицит";

    private static string CapitalizeFirstLetter(string title)
        => title.Length == 0 ? title : char.ToUpperInvariant(title[0]) + title[1..];

    private static decimal Total(BudgetPeriodSums period) => period.Income - period.Expense;
}
