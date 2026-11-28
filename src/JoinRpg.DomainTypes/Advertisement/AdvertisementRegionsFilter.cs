namespace JoinRpg.DomainTypes.Advertisement;

/// <summary>
/// Разбор строкового фильтра регионов расписания рекламы (<c>AdvertisementScheduleEntity.RegionsFilter</c>):
/// список числовых id субрегионов КогдаИгры через запятую (например <c>"1,2,33"</c>).
/// </summary>
public static class AdvertisementRegionsFilter
{
    /// <summary>
    /// null или пустая строка — фильтра нет (все регионы). Иначе — множество регионов;
    /// токены, не разбирающиеся в id из справочника, пропускаются, поэтому мусорная строка
    /// даёт пустое множество (фильтр задан, но ничему не соответствует), а не «все регионы».
    /// </summary>
    public static IReadOnlySet<Region>? Parse(string? regionsFilter)
    {
        if (string.IsNullOrWhiteSpace(regionsFilter))
        {
            return null;
        }

        var regions = new HashSet<Region>();
        foreach (var token in regionsFilter.Split(','))
        {
            if (int.TryParse(token.Trim(), out var id) && Enum.IsDefined(typeof(Region), id))
            {
                regions.Add((Region)id);
            }
        }

        return regions;
    }
}
