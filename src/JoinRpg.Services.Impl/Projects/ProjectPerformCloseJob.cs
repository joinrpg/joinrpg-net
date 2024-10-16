using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Services.Impl.Projects;
public class ProjectPerformCloseJob(IProjectRepository projectRepository, ILogger<ProjectPerformCloseJob> logger, IProjectService projectService) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var staleProjects = await projectRepository.GetStaleProjects(today.AddMonths(-6).ToDateTime(TimeOnly.MinValue));

        foreach (var staleProject in staleProjects)
        {
            var lastUpdateDate = DateOnly.FromDateTime(staleProject.LastUpdated);
            DateOnly closeDate = ProjectStaleDateCalculator.CalculateCloseDate(lastUpdateDate);

            if (today != closeDate)
            {
                logger.LogInformation("Project {project} is stale since {staleDate}. It will be closed later on {closeDate}.", staleProject.ProjectId, lastUpdateDate, closeDate);
                continue;
            }
            else
            {
                logger.LogInformation("Project {project} is stale since {staleDate}. It will be closed now.", staleProject.ProjectId, lastUpdateDate);
            }

            await projectService.CloseProjectAsStale(new(staleProject.ProjectId), lastUpdateDate);
        }

    }
}
