using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using ProjectRolesList = JoinRpg.DataModel.ProjectRolesList;

namespace JoinRpg.Services.Impl.Projects;

/// <summary>
/// Добавляет публичную сетку «Горячие роли» всем активным проектам, у которых есть хотя бы одна
/// горячая роль и ещё нет такой сетки. Аналог того, что <see cref="CreateProjectService"/> делает
/// для новых Larp-проектов, но для уже существующих игр.
/// </summary>
public class AddHotRolesListJob(
    IUnitOfWork unitOfWork,
    IProjectMetadataRepository projectMetadataRepository,
    IProjectRolesListService projectRolesListService,
    ILogger<AddHotRolesListJob> logger) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
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
            await projectRolesListService.CreateAsync(
                HotRolesListDefaults.Build(projectId, metadata.CharacterDescriptionField?.Id));
        }

        logger.LogInformation("Добавлена сетка «Горячие роли» в {Count} проектов", projectIds.Count);
    }
}
