using System.Globalization;

namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Форматирование периода дат для отображения пользователю.
/// </summary>
public static class DateRangeFormatter
{
    /// <summary>
    /// Форматирует период дат для отображения.
    /// Примеры: «1 марта 2026», «1–5 марта 2026», «1 марта–5 апреля 2026», «01.03.2026–05.04.2027».
    /// </summary>
    public static string FormatDisplay(DateRange range, CultureInfo? culture = null, bool hideCurrentYear = false)
    {
        culture ??= CultureInfo.CurrentCulture;

        var (begin, end) = range;

        if (begin.Year != end.Year)
        {
            return $"{begin.ToString("d", culture)}–{end.ToString("d", culture)}";
        }

        var yearSuffix = hideCurrentYear && begin.Year == DateTime.Now.Year
            ? string.Empty
            : $" {begin.Year}";

        if (begin.Month == end.Month && begin.Day == end.Day)
        {
            return $"{begin.ToString("d MMMM", culture)}{yearSuffix}";
        }

        if (begin.Month == end.Month)
        {
            return $"{begin.Day}–{end.ToString("d MMMM", culture)}{yearSuffix}";
        }

        return $"{begin.ToString("d MMMM", culture)}–{end.ToString("d MMMM", culture)}{yearSuffix}";
    }

    /// <summary>
    /// Форматирует дни недели периода дат для всплывающей подсказки.
    /// Примеры: «воскресенье», «воскресенье–четверг».
    /// </summary>
    public static string FormatTooltip(DateRange range, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        var (begin, end) = range;

        if (begin == end)
        {
            return begin.ToString("dddd", culture);
        }

        return $"{begin.ToString("dddd", culture)}–{end.ToString("dddd", culture)}";
    }
}
