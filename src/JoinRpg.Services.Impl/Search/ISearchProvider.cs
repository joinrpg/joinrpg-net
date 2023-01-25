using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal interface ISearchProvider
{
    Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString);
}
