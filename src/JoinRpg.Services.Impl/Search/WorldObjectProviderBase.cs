using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal class WorldObjectProviderBase
{
    protected static List<SearchResult> GetWorldObjectsResult(
      int? currentUserId,
      IEnumerable<IWorldObject> results,
      LinkType linkType,
      Predicate<IWorldObject> wasFoundByIdPredicate,
      Predicate<IWorldObject> perfectMatchPredicte)
    {
        return [.. results.Where(cg => cg.IsVisible(currentUserId))
          .Select(result =>
            new SearchResult
            {
                LinkType = linkType,
                Name = result.Name,
                Description = wasFoundByIdPredicate(result)
                ? SearchUtils.GetFoundByIdDescription(result.Id)
                : result.Description,
                Identification = result.Id.ToString(),
                ProjectId = result.ProjectId,
                IsPublic = result.IsPublic,
                IsActive = result.IsActive,
                IsPerfectMatch = perfectMatchPredicte(result),
            })];
    }

    /// <summary>
    /// Checks for master access is it's a match by Id
    /// </summary>
    protected static bool CheckMasterAccessIfMatchById(
      IProjectEntity entity,
      int? currentUserId,
      int? searchedEntityId)
    {
        return
          entity.Id != searchedEntityId
          || entity.Project.HasMasterAccess(currentUserId);
    }
}
