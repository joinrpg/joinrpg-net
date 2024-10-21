using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Services.Impl.Projects;
public class ProjectWarnCloseJob(IProjectRepository projectRepository, IMasterEmailService masterEmailService, ILogger<ProjectWarnCloseJob> logger) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        DateTime today = DateTime.Now.Date;
        var staleProjects = await projectRepository.GetStaleProjects(today.AddMonths(-6));

        foreach (var staleProject in staleProjects)
        {
            if (staleProject.LastUpdated.Day != today.Day)
            {
                logger.LogInformation("Project {project} is stale since {staleDate}. It will be warned on {dayOfMonth} day of month",
                    staleProject.ProjectId,
                    staleProject.LastUpdated,
                    staleProject.LastUpdated.Day);
                continue;
            }

            var lastUpdateDate = DateOnly.FromDateTime(staleProject.LastUpdated);
            DateOnly closeDate = ProjectStaleDateCalculator.CalculateCloseDate(lastUpdateDate);

            if (closeDate == DateOnly.FromDateTime(DateTime.Now.Date))
            {
                logger.LogInformation("Project {project} is stale since {staleDate}. Skipping warning, because it will be closed today.",
                    staleProject.ProjectId,
                    staleProject.LastUpdated);
            }
            var email = new ProjectStaleMail()
            {
                LastActiveDate = lastUpdateDate,
                ProjectId = new(staleProject.ProjectId),
                WillCloseDate = closeDate,
            };

            await masterEmailService.EmailProjectStale(email);
        }

    }
}
