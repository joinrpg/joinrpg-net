using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
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
      var searchTasks = GetProviders().Select(p => p.SearchAsync(searchString)).ToList(); //Starting searches
      var results = new List<ISearchResult>();
      foreach (var bucket in searchTasks.Interleaved())
      {
        var task = await bucket;
        var rGroup = await task;
        //TODO: We can stop here when we have X results.
        results.AddRange(rGroup);
      }
      return results.AsReadOnly(); 
    }

    private IEnumerable<ISearchProvider> GetProviders()
    {
      yield return new UserSearchProvider {UnitOfWork = UnitOfWork};
    }
  }

  internal class UserSearchProvider : ISearchProvider
  {
    public IUnitOfWork UnitOfWork { private get; set; }

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(string searchString)
    {
      var results =
        await
          UnitOfWork.GetDbSet<User>()
            .Where(user =>
             //TODO There should be magic way to do this. Experiment with Expression.Voodoo
              user.Email.Contains(searchString)
              || user.FatherName.Contains(searchString)
              || user.BornName.Contains(searchString)
              || user.SurName.Contains(searchString)
            )
            .ToListAsync();

      return results.Select(user => new SearchResultImpl
            {
              Type = SearchResultType.ResultUser,
              Name = user.DisplayName,
              Description = "",
              FoundValue = user.Email,
              Identification = user.UserId.ToString()
            }).ToList();
    }
  }

  internal class SearchResultImpl : ISearchResult
  {
    public SearchResultType Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string FoundValue { get; set; }
    public string Identification { get; set; }
    public int? ProjectId => null; //Users not associated with any project
  }

  internal interface ISearchProvider
  {
    Task<IReadOnlyCollection<ISearchResult>> SearchAsync(string searchString);
  }
}
