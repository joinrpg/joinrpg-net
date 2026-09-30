using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal class WorldObjectProviderBase(IProjectMetadataRepository projectMetadataRepository)
{
    /// <summary>
    /// Отбирает находки, которые можно показать пользователю, и превращает их в результаты поиска.
    /// </summary>
    /// <remarks>
    /// Видимость и мастерский доступ считаются поверх <see cref="ProjectInfo"/>, а не по EF-навигациям
    /// (<c>Project</c>/<c>Project.Details</c>/<c>Project.ProjectAcls</c>): иначе каждая скрытая находка
    /// стоила бы тройку ленивых загрузок, а результаты глобального поиска — это N объектов из разных
    /// проектов (#4991). Метаданные кешируются на запрос, и берутся они по одному разу на проект.
    /// </remarks>
    protected async Task<List<SearchResult>> GetWorldObjectsResultAsync(
      int? currentUserId,
      IReadOnlyCollection<IWorldObject> results,
      LinkType linkType,
      int? searchedEntityId,
      bool matchByIdIsPerfect = false)
    {
        var currentUser = UserIdentification.FromOptional(currentUserId);
        var projects = await LoadProjectsMetadataAsync(results);

        var searchResults = new List<SearchResult>();
        foreach (var result in results)
        {
            var projectInfo = projects[new ProjectIdentification(result.ProjectId)];
            var wasFoundById = searchedEntityId is not null && result.Id == searchedEntityId;
            var hasMasterAccess = projectInfo.HasMasterAccess(currentUser);

            // Поиск по id — только для мастеров проекта находки
            if (wasFoundById && !hasMasterAccess)
            {
                continue;
            }

            // Аналог WorldObjectExtensions.IsVisible, но без обращения к EF-навигациям
            if (!result.IsPublic && !projectInfo.PublishPlot && !hasMasterAccess)
            {
                continue;
            }

            searchResults.Add(new SearchResult
            {
                LinkType = linkType,
                Name = result.Name,
                Description = wasFoundById
                  ? SearchUtils.GetFoundByIdDescription(result.Id)
                  : result.Description,
                Identification = result.Id.ToString(),
                ProjectId = result.ProjectId,
                IsPublic = result.IsPublic,
                IsActive = result.IsActive,
                IsPerfectMatch = wasFoundById && matchByIdIsPerfect,
            });
        }

        return searchResults;
    }

    private async Task<Dictionary<ProjectIdentification, ProjectInfo>> LoadProjectsMetadataAsync(
      IEnumerable<IProjectEntity> entities)
    {
        var projects = new Dictionary<ProjectIdentification, ProjectInfo>();
        foreach (var projectId in entities.Select(e => new ProjectIdentification(e.ProjectId)).Distinct())
        {
            projects[projectId] = await projectMetadataRepository.GetProjectMetadata(projectId);
        }

        return projects;
    }
}
