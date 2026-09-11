using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Interfaces;

public interface ISearchService
{
    Task<IReadOnlyCollection<SearchResult>> SearchAsync(
        int? currentUserId,
        string searchString,
        IReadOnlyCollection<LinkType>? linkTypes = null,
        ProjectIdentification? projectId = null);
}
