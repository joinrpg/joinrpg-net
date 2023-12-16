using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Impl.Search;

internal class WorldObjectProviderBase
{
    protected static List<SearchResultImpl> GetWorldObjectsResult(
      int? currentUserId,
      IEnumerable<IWorldObject> results,
      LinkType linkType,
      Predicate<IWorldObject> wasFoundByIdPredicate,
      Predicate<IWorldObject> perfectMatchPredicte)
    {
        return results.Where(cg => cg.IsVisible(currentUserId))
          .Select(result =>
            new SearchResultImpl
            {
                LinkType = linkType,
                Name = result.Name,
                Description = wasFoundByIdPredicate(result)
                ? new MarkdownString(GetFoundByIdDescription(result.Id))
                : result.Description,
                Identification = result.Id.ToString(),
                ProjectId = result.ProjectId,
                IsPublic = result.IsPublic,
                IsActive = result.IsActive,
                IsPerfectMatch = perfectMatchPredicte(result),
            })
          .ToList();
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

    public static string GetFoundByIdDescription(int idToFind) => $"ID: {idToFind}";
}
