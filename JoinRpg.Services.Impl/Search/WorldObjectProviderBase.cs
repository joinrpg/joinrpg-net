using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Services.Impl.Search
{
  internal class WorldObjectProviderBase
  {
    protected static List<SearchResultImpl> GetWorldObjectsResult(
      int? currentUserId,
      [InstantHandle] IEnumerable<IWorldObject> results,
      LinkType linkType,
      [InstantHandle] Predicate<IWorldObject> perfectMatchPredicte)
    {
      return results.Where(cg => cg.IsVisible(currentUserId))
        .Select(@result => SearchResultImpl.FromWorldObject(@result, linkType, perfectMatchPredicte(@result)))
        .ToList();
    }

    /// <summary>
    /// Checks for master access is it's a match by Id
    /// </summary>
    /// <returns></returns>
    protected static bool CheckMasterAccessIfMatchById(
      IProjectEntity entity,
      int? currentUserId,
      int? searchedEntityId)
    {
      return
        entity.Id != searchedEntityId
        || entity.Project.HasMasterAccess(currentUserId);
    }

    public IUnitOfWork UnitOfWork { protected get; set; }
  }
}
