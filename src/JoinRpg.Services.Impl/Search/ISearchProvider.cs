using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal interface ISearchProvider
{
    Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString);
}
