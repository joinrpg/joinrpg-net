using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal class SearchServiceImpl(IEnumerable<ISearchProvider> searchProviders) : ISearchService
{
    public async Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
        searchString = searchString.Trim();
        if (searchString.Length == 0)
        {
            return [];
        }

        var searchTasks = searchProviders.Select(p => p.SearchAsync(currentUserId, searchString));

        var results = new List<SearchResult>();
        foreach (var task in searchTasks)
        {
            var rGroup = await task;
            //TODO: We can stop here when we have X results.
            results.AddRange(rGroup);

            // If there're results that perfectly match the search string - return them only.
            // e.g. контакты123 should return only useer with ID=123
            if (rGroup.Any(r => r.IsPerfectMatch))
            {
                return Deduplicate(results.Where(r => r.IsPerfectMatch));
            }
        }


        return Deduplicate(results);
    }

    /// <summary>
    /// Разные провайдеры могут найти один и тот же объект (пользователя, персонажа, заявку)
    /// по разным признакам (например по id и по ссылке на VK) и заполнить Description
    /// по-разному. Схлопываем такие результаты по бизнес-ключу (LinkType, Identification)
    /// вместо Distinct() по всему record, объединяя все найденные Description, а не выбрасывая
    /// часть из них.
    /// </summary>
    private static IReadOnlyCollection<SearchResult> Deduplicate(IEnumerable<SearchResult> results)
        => [.. results
            .GroupBy(r => (r.LinkType, r.Identification))
            .Select(g => g.Count() == 1
                ? g.First()
                : g.First() with
                {
                    Description = MergeDescriptions(g),
                    IsPerfectMatch = g.Any(r => r.IsPerfectMatch),
                })];

    private static MarkdownDbValue MergeDescriptions(IEnumerable<SearchResult> group)
    {
        var parts = group
            .Select(r => r.Description.Contents)
            .Where(contents => !string.IsNullOrWhiteSpace(contents))
            .Distinct()
            .ToArray();

        return parts.Length == 0 ? new MarkdownDbValue() : new MarkdownDbValue(string.Join(", ", parts));
    }
}
