using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal class WorldObjectProviderBase(IProjectMetadataRepository projectMetadataRepository)
{
    /// <summary>
    /// Провайдер обрабатывает много результатов одного проекта подряд — локальный словарь
    /// не даёт даже строить лишние Task'и (сам репозиторий всё равно кеширует на запрос).
    /// </summary>
    private readonly Dictionary<ProjectIdentification, Task<ProjectInfo>> projectInfoCache = new();

    private Task<ProjectInfo> GetProjectInfo(IProjectEntity entity)
    {
        var projectId = entity.ProjectIdentification;
        if (!projectInfoCache.TryGetValue(projectId, out var projectInfoTask))
        {
            projectInfoTask = projectMetadataRepository.GetProjectMetadata(projectId);
            projectInfoCache[projectId] = projectInfoTask;
        }
        return projectInfoTask;
    }

    protected async Task<List<SearchResult>> GetWorldObjectsResult(
      int? currentUserId,
      IEnumerable<IWorldObject> results,
      LinkType linkType,
      Predicate<IWorldObject> wasFoundByIdPredicate,
      Predicate<IWorldObject> perfectMatchPredicte)
    {
        var userId = UserIdentification.FromOptional(currentUserId);
        var searchResults = new List<SearchResult>();
        foreach (var result in results)
        {
            var projectInfo = await GetProjectInfo(result);
            if (!result.IsVisible(projectInfo, userId))
            {
                continue;
            }

            searchResults.Add(
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
              });
        }
        return searchResults;
    }

    /// <summary>
    /// Checks for master access is it's a match by Id
    /// </summary>
    protected async Task<bool> CheckMasterAccessIfMatchById(
      IProjectEntity entity,
      int? currentUserId,
      int? searchedEntityId)
    {
        if (entity.Id != searchedEntityId)
        {
            return true;
        }

        // Проверка доступа поверх ProjectInfo (кеш на запрос), а не entity.Project.ProjectAcls —
        // иначе на каждый найденный объект лениво догружаются Projects и ProjectAcls (#4991).
        var projectInfo = await GetProjectInfo(entity);
        return projectInfo.HasMasterAccess(UserIdentification.FromOptional(currentUserId));
    }
}
