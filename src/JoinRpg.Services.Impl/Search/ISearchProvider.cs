using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal interface ISearchProvider
{
    LinkType LinkType { get; }

    Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString);
}
