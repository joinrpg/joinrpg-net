using System.Collections.Generic;
using System.Linq;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Services.Impl.Search
{
  internal class WorldObjectProviderBase
  {
    protected static List<SearchResultImpl> GetWorldObjectsResult(int? currentUserId, IEnumerable<IWorldObject> results)
    {
      return results.Where(cg => cg.IsVisible(currentUserId))
        .Select(@group => SearchResultImpl.FromWorldObject(@group, LinkType.ResultCharacter))
        .ToList();
    }

    public IUnitOfWork UnitOfWork { protected get; set; }
  }
}
