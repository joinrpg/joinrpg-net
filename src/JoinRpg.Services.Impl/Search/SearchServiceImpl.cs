using JoinRpg.Services.Interfaces;
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
                return [.. results.Where(r => r.IsPerfectMatch).Distinct()];
            }
        }


        return [.. results.Distinct()];
    }
}
