using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Interfaces
{
  public interface ISearchService
  {
    Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString);
  }
}