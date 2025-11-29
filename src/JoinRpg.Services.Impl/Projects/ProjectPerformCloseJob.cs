using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;
internal class ProjectPerformCloseJob(
    IProjectRepository projectRepository,
    ILogger<ProjectPerformCloseJob> logger,
    IProjectService projectService,
    IMasterEmailService masterEmailService
    ) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var staleProjects = await projectRepository.GetStaleProjects(today.AddMonths(-3).ToDateTime(TimeOnly.MinValue));

        foreach (var staleProject in staleProjects)
        {
            var lastUpdateDate = DateOnly.FromDateTime(staleProject.LastUpdated);
            var (status, closeDate) = ProjectStaleDateCalculator.CalculateStaleStatus(lastUpdateDate, today);

            logger.LogInformation("Project {project} is stale candidate. NotActive since {lastUpdated}, Status {staleStatus}, CloseDate {closeDate}.",
                staleProject.ProjectId,
                staleProject.LastUpdated,
                status,
                closeDate);

            switch (status)
            {
                case ProjectStaleStatus.NotStale:
                    continue;
                case ProjectStaleStatus.StaleWarnToday:
                    var email = new ProjectStaleMail()
                    {
                        LastActiveDate = lastUpdateDate,
                        ProjectId = new(staleProject.ProjectId),
                        WillCloseDate = closeDate,
                    };

                    logger.LogInformation("Sending stale warning for project {project}", staleProject.ProjectId);
                    await masterEmailService.EmailProjectStale(email);
                    continue;
                case ProjectStaleStatus.StaleWarnOtherTime:
                    continue;
                case ProjectStaleStatus.StaleCloseToday:
                    logger.LogInformation("Project {project} is stale since {staleDate}. It will be closed now.", staleProject.ProjectId, lastUpdateDate);
                    await projectService.CloseProjectAsStale(new(staleProject.ProjectId), lastUpdateDate);
                    continue;
            }
        }

    }
}
