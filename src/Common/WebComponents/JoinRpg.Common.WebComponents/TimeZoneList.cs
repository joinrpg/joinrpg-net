using System.Globalization;

namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Часовой пояс в списке <see cref="TimeZoneSelector"/>.
/// </summary>
/// <param name="Id">IANA-идентификатор, например <c>Europe/Moscow</c>.</param>
/// <param name="BaseUtcOffset">Стандартное (без летнего времени) смещение от UTC.</param>
public record TimeZoneListItem(string Id, TimeSpan BaseUtcOffset)
{
    public string DisplayName { get; } = $"(UTC{FormatOffset(BaseUtcOffset)}) {Id}";

    private static string FormatOffset(TimeSpan offset)
        => (offset < TimeSpan.Zero ? "-" : "+") + offset.Duration().ToString(@"hh\:mm", CultureInfo.InvariantCulture);
}

/// <summary>
/// Все часовые пояса с IANA-идентификатором, отсортированные по смещению от UTC, затем по идентификатору.
/// </summary>
public static class TimeZoneList
{
    private static readonly Lazy<TimeZoneListItem[]> all = new(Load);

    public static IReadOnlyList<TimeZoneListItem> All => all.Value;

    /// <summary>
    /// Список для селектора. Если выбранного пояса в системном списке нет (например, устаревший псевдоним),
    /// он добавляется, чтобы выбор не потерялся молча.
    /// </summary>
    public static TimeZoneListItem[] WithSelected(string? selectedId)
    {
        if (string.IsNullOrWhiteSpace(selectedId) || all.Value.Any(z => z.Id == selectedId))
        {
            return all.Value;
        }
        var offset = TimeZoneInfo.TryFindSystemTimeZoneById(selectedId, out var zone) ? zone.BaseUtcOffset : TimeSpan.Zero;
        return Sort(all.Value.Append(new TimeZoneListItem(selectedId, offset)));
    }

    private static TimeZoneListItem[] Load()
    {
        var items = TimeZoneInfo.GetSystemTimeZones()
            .Select(zone => (zone, ianaId: ToIanaId(zone)))
            .Where(x => x.ianaId is not null)
            .DistinctBy(x => x.ianaId)
            .Select(x => new TimeZoneListItem(x.ianaId!, x.zone.BaseUtcOffset));
        return Sort(items);
    }

    // На Linux и в WASM системные пояса уже в IANA. На Windows (локальная разработка) — в Windows-идентификаторах,
    // их переводим в основной IANA-пояс.
    private static string? ToIanaId(TimeZoneInfo zone)
    {
        if (zone.HasIanaId)
        {
            return zone.Id;
        }
        return TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var ianaId) ? ianaId : null;
    }

    private static TimeZoneListItem[] Sort(IEnumerable<TimeZoneListItem> items)
        => [.. items.OrderBy(z => z.BaseUtcOffset).ThenBy(z => z.Id, StringComparer.Ordinal)];
}
