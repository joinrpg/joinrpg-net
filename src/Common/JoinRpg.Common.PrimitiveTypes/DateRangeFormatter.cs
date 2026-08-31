using System.Globalization;

namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Форматирует период дат для отображения пользователю на русском языке.
/// </summary>
public static class DateRangeFormatter
{
    private static readonly CultureInfo russianCulture = CultureInfo.GetCultureInfo("ru-RU");

    /// <summary>
    /// Возвращает строку с периодом дат на русском языке.
    /// </summary>
    /// <param name="appendYearWord">Добавлять ли слово «г» после года (например, «2027 г»).</param>
    public static string Format(DateOnly start, DateOnly end, bool hideCurrentYear = false, bool appendYearWord = false)
    {
        if (start.Year != end.Year)
        {
            return $"{start.ToString("dd.MM.yyyy", russianCulture)}–{end.ToString("dd.MM.yyyy", russianCulture)}";
        }

        var year = hideCurrentYear && start.Year == DateTime.Now.Year
            ? string.Empty
            : $" {start.Year}";
        var yearSuffix = appendYearWord && !string.IsNullOrEmpty(year) ? $"{year} г" : year;

        if (start.Month == end.Month && start.Day == end.Day)
        {
            return $"{start.ToString("d MMMM", russianCulture)}{yearSuffix}";
        }

        if (start.Month == end.Month)
        {
            return $"{start.Day}–{end.ToString("d MMMM", russianCulture)}{yearSuffix}";
        }

        return $"{start.ToString("d MMMM", russianCulture)}–{end.ToString("d MMMM", russianCulture)}{yearSuffix}";
    }
}
