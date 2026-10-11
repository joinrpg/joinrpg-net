using System.Text.Json;

namespace JoinRpg.DomainTypes.Characters;

/// <summary>
/// Замена пользователя в значениях полей «ссылка на пользователя» (ADR017) — для слияния
/// пользователей (ADR024). Работает с сырым <c>JsonData</c> персонажа или заявки: без
/// <see cref="ProjectInfo"/>, поэтому годится для массовой правки в DAL.
/// </summary>
public static class UserLinkFieldRewriter
{
    /// <param name="jsonData">Значения полей: id поля → сырое значение.</param>
    /// <param name="userLinkFieldIds">Поля проекта типа «ссылка на пользователя»; остальные не трогаются.</param>
    /// <returns>Новый <c>JsonData</c> или <c>null</c>, если заменять было нечего.</returns>
    public static string? Replace(
        string? jsonData,
        IReadOnlySet<int> userLinkFieldIds,
        UserIdentification from,
        UserIdentification to)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            return null;
        }
        var values = JsonSerializer.Deserialize<Dictionary<int, string?>>(jsonData) ?? [];

        var changed = false;
        foreach (var (fieldId, value) in values.Where(v => userLinkFieldIds.Contains(v.Key)).ToList())
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var parts = value.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
            // Сравниваем числом, как читает FieldWithValue: "05" — тоже пользователь 5.
            bool IsFrom(string part) => int.TryParse(part, out var id) && id == from.Value;
            if (!parts.Any(IsFrom))
            {
                continue;
            }

            // Мультиполе могло называть обоих — после слияния это один человек.
            values[fieldId] = string.Join(",", parts.Select(p => IsFrom(p) ? to.Value.ToString() : p).Distinct());
            changed = true;
        }

        return changed ? JsonSerializer.Serialize(values) : null;
    }
}
