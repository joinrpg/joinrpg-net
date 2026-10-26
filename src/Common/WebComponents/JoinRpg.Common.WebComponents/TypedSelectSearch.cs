using System.Globalization;
using System.Text;

namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Поиск по пунктам <see cref="TypedSelector{TKey, TItem}"/>. Строку пункта нормализует сервер и кладёт в
/// <c>data-search</c>: по ней фильтрует и C# в интерактивном режиме, и делегированный слушатель
/// в <c>JoinRpg.Common.WebComponents.lib.module.js</c> в статическом. Нормализация запроса в JS
/// повторяет <see cref="Normalize"/> — менять их надо вместе.
/// </summary>
public static class TypedSelectSearch
{
    /// <summary>
    /// Нижний регистр, «ё» как «е», без диакритики (как <c>data-live-search-normalize</c> у bootstrap-select:
    /// «й» тоже становится «и»), пробелы схлопнуты.
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        string decomposed;
        var wellFormed = true;
        try
        {
            decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        }
        catch (ArgumentException)
        {
            // Непарный суррогат (имя, обрезанное посреди эмодзи) — ищем по тексту как есть, но не роняем страницу.
            decomposed = text.ToLowerInvariant();
            wellFormed = false;
        }

        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;
        foreach (var ch in decomposed)
        {
            if (IsCombiningMark(ch))
            {
                continue;
            }
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }
            if (pendingSpace)
            {
                _ = builder.Append(' ');
                pendingSpace = false;
            }
            _ = builder.Append(ch);
        }
        var result = builder.ToString();
        return wellFormed ? result.Normalize(NormalizationForm.FormC) : result;
    }

    /// <summary>Строка, по которой ищется пункт: текст, подсказка и дополнительные слова для поиска.</summary>
    public static string ForItem<TKey>(TypedSelectListItem<TKey> item) where TKey : notnull
        => Normalize($"{item.Text} {item.Subtext} {item.ExtraSearch}");

    /// <summary>Пункт подходит, если в его строке есть каждое слово запроса.</summary>
    /// <param name="itemSearch">Уже нормализованная строка пункта (<see cref="ForItem{TKey}"/>).</param>
    /// <param name="query">Запрос как ввёл пользователь.</param>
    public static bool Matches(string itemSearch, string? query)
        => Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries).All(itemSearch.Contains);

    // Диапазон «Combining Diacritical Marks»: в нём и бреве у «й», и диерезис у «ё». JS берёт тот же диапазон.
    private static bool IsCombiningMark(char ch) => ch is >= '̀' and <= 'ͯ'
        && CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark;
}
