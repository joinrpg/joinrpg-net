using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

[UsedImplicitly]
public class SearchServiceImpl : DbServiceImplBase, ISearchService
{
    public SearchServiceImpl(IUnitOfWork unitOfWork, ICurrentUserAccessor currentUserAccessor) : base(unitOfWork, currentUserAccessor)
    {
    }

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
        var results = new List<ISearchResult>();
        //TODO: We like to do multiple searches in parallel. We only allowed it to do in parallel UnitOfWorks
        foreach (var task in GetProviders().Select(p => p.SearchAsync(currentUserId, searchString)))
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

    private IEnumerable<ISearchProvider> GetProviders()
    {
        yield return new UserSearchProvider { UnitOfWork = UnitOfWork };
        yield return new CharacterGroupsProvider { UnitOfWork = UnitOfWork };
        yield return new CharacterProvider { UnitOfWork = UnitOfWork };
        yield return new PlotSearchProvider { UnitOfWork = UnitOfWork };
        yield return new ProjectSearchProvider { UnitOfWork = UnitOfWork };
        yield return new ClaimsByIdProvider { UnitOfWork = UnitOfWork };
    }
}

internal class SearchResultImpl : ISearchResult
{
    public LinkType LinkType { get; set; }
    public string Name { get; set; }
    public MarkdownString Description { get; set; }

    public bool IsPublic { get; set; }

    public bool IsActive { get; set; }
    public bool IsPerfectMatch { get; set; } = false;

    public string Identification { get; set; }
    public int? ProjectId { get; set; }
}

internal interface ISearchProvider
{
    Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString);
}
