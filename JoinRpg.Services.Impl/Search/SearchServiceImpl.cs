using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search
{
  [UsedImplicitly]
  public class SearchServiceImpl : DbServiceImplBase, ISearchService
  {
    public SearchServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(string searchString)
    {
      var results = new List<ISearchResult>();
      //TODO: We like to do multiple searches in parallel. We only allowed it to do in parallel UnitOfWorks
      foreach (var task in GetProviders().Select(p => p.SearchAsync(searchString)))
      {
        var rGroup = await task;
        //TODO: We can stop here when we have X results.
        results.AddRange(rGroup);
      }
      return results.AsReadOnly(); 
    }

    private IEnumerable<ISearchProvider> GetProviders()
    {
      yield return new UserSearchProvider {UnitOfWork = UnitOfWork};
      yield return new PublicCharacterGroupsProvider { UnitOfWork = UnitOfWork };
      yield return new PublicCharacterProvider { UnitOfWork = UnitOfWork };
    }
  }

  internal class SearchResultImpl : ISearchResult
  {
    public SearchResultType Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Identification { get; set; }
    public int? ProjectId {get;set;}

    public static SearchResultImpl FromWorldObject(IWorldObject @group, SearchResultType type)
    {
      return new SearchResultImpl
      {
        Type = type,
        Name = @group.Name,
        Description = "",
        Identification = @group.Id.ToString(),
        ProjectId = @group.ProjectId
      };
    }
  }

  internal interface ISearchProvider
  {
    Task<IReadOnlyCollection<ISearchResult>> SearchAsync(string searchString);
  }
}
