using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using ProjectRolesList = JoinRpg.DataModel.ProjectRolesList;

namespace JoinRpg.Services.Impl.Projects;

/// <summary>
/// Гарантирует, что у активных проектов есть хотя бы одна публичная сетка ролей: добавляет
/// сетку «Все роли» проектам, у которых ещё нет ни одной сетки, и сетку «Горячие роли»
/// проектам с горячими ролями, у которых такой сетки ещё нет. Аналог того, что
/// <see cref="CreateProjectService"/> делает при создании нового проекта, но для уже
/// существующих игр.
/// </summary>
public class EnsureRolesListsJob(
    IUnitOfWork unitOfWork,
    IProjectMetadataRepository projectMetadataRepository,
    IProjectRolesListService projectRolesListService,
    ILogger<EnsureRolesListsJob> logger) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        await AddAllRolesListToProjectsWithoutAnyList(cancellationToken);
        await AddHotRolesListToProjectsWithHotCharacters(cancellationToken);
    }

    private async Task AddAllRolesListToProjectsWithoutAnyList(CancellationToken cancellationToken)
    {
        // Активные проекты.
        var activeProjectIds = await unitOfWork.GetDbSet<Project>()
            .Where(p => p.Active)
            .Select(p => p.ProjectId)
            .ToListAsync(cancellationToken);

        // Проекты, у которых уже есть хотя бы одна сетка ролей — их пропускаем (идемпотентность).
        var projectsWithLists = await unitOfWork.GetDbSet<ProjectRolesList>()
            .Select(l => l.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var projectIds = activeProjectIds
            .Except(projectsWithLists)
            .OrderBy(id => id)
            .ToList();

        if (projectIds.Count == 0)
        {
            logger.LogInformation("Нет проектов без сеток ролей");
            return;
        }

        foreach (var id in projectIds)
        {
            var projectId = new ProjectIdentification(id);
            var metadata = await projectMetadataRepository.GetProjectMetadata(projectId);

            logger.LogInformation("Добавляем сетку «Все роли» в проект {ProjectId}", projectId);

            // Джоба выполняется под роботом-админом, поэтому ProjectPropsService, через который
            // работает CreateAsync, пропускает проверку мастер-прав.
            IReadOnlyList<ProjectFieldIdentification> fields = (metadata.CharacterDescriptionField?.Id is not null) ? [metadata.CharacterDescriptionField.Id] : [];
            await projectRolesListService.CreateAsync(new DomainTypes.ProjectMetadata.ProjectRolesList(projectId, "Все роли", fields, RolesGridGroupsViewMode.Tree, ShowRolesFilter.All));
        }

        logger.LogInformation("Добавлена сетка «Все роли» в {Count} проектов", projectIds.Count);
    }

    private async Task AddHotRolesListToProjectsWithHotCharacters(CancellationToken cancellationToken)
    {
        // Активные проекты, у которых есть хотя бы одна активная горячая роль.
        var candidateProjectIds = await unitOfWork.GetDbSet<Character>()
            .Where(c => c.IsHot && c.IsActive && c.Project.Active)
            .Select(c => c.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Проекты, у которых сетка с горячими ролями уже есть — их пропускаем (идемпотентность).
        var projectsWithHotList = await unitOfWork.GetDbSet<ProjectRolesList>()
            .Where(l => l.ShowRolesFilter == ShowRolesFilter.HotOnly)
            .Select(l => l.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var projectIds = candidateProjectIds
            .Except(projectsWithHotList)
            .OrderBy(id => id)
            .ToList();

        if (projectIds.Count == 0)
        {
            logger.LogInformation("Нет проектов, которым нужно добавить сетку «Горячие роли»");
            return;
        }

        foreach (var id in projectIds)
        {
            var projectId = new ProjectIdentification(id);
            var metadata = await projectMetadataRepository.GetProjectMetadata(projectId);

            logger.LogInformation("Добавляем сетку «Горячие роли» в проект {ProjectId}", projectId);

            // Джоба выполняется под роботом-админом, поэтому ProjectPropsService, через который
            // работает CreateAsync, пропускает проверку мастер-прав.

            IReadOnlyList<ProjectFieldIdentification> fields = (metadata.CharacterDescriptionField?.Id is not null) ? [metadata.CharacterDescriptionField.Id] : [];
            await projectRolesListService.CreateAsync(new DomainTypes.ProjectMetadata.ProjectRolesList(projectId, "Горячие роли", fields, RolesGridGroupsViewMode.None, ShowRolesFilter.HotOnly));

        }

        logger.LogInformation("Добавлена сетка «Горячие роли» в {Count} проектов", projectIds.Count);
    }
}
