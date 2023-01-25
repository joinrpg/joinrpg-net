using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal class SearchServiceImpl : ISearchService
{
    private readonly IEnumerable<ISearchProvider> searchProviders;

    public SearchServiceImpl(IEnumerable<ISearchProvider> searchProviders)
    {
        this.searchProviders = searchProviders;
    }

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
        var results = new List<ISearchResult>();
        //TODO: We like to do multiple searches in parallel.
        //For it we need to convert all searchproviders to AsNoTracking()
        foreach (var task in searchProviders.Select(p => p.SearchAsync(currentUserId, searchString)))
        {
            var rGroup = await task;
            //TODO: We can stop here when we have X results.
            results.AddRange(rGroup);
        }

        // If there're results that perfectly match the search string - return them only. 
        // e.g. контакты123 should return only useer with ID=123
        return results.Any(r => r.IsPerfectMatch)
          ? results.Where(r => r.IsPerfectMatch).ToList().AsReadOnly()
          : results.AsReadOnly();
    }
}
