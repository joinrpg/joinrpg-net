using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
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
      var searchTasks = GetProviders().Select(p => p.SearchAsync(searchString)).ToList(); //Starting searches
      var result = await Task.WhenAll(searchTasks);
      return result.SelectMany(resultGroup => resultGroup).ToList().AsReadOnly(); //Waiting for results
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
      return
        await
          UnitOfWork.GetDbSet<User>()
            .Where(user => user.Email.Contains(searchString))
            .Select(user => new SearchResultImpl
            {
              Type = SearchResultType.ResultUser,
              Name = user.UserName,
              Description = "",
              FoundValue = user.Email,
              Identification = user.UserId.ToString()
            }).ToListAsync();
    }
  }

  internal class SearchResultImpl : ISearchResult
  {
    public SearchResultType Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string FoundValue { get; set; }
    public string Identification { get; set; }
  }

  internal interface ISearchProvider
  {
    Task<IReadOnlyCollection<ISearchResult>> SearchAsync(string searchString);
    IUnitOfWork UnitOfWork { set; }
  }
}
