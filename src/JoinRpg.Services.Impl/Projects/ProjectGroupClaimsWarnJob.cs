using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Services.Impl.Projects;
public class ProjectGroupClaimsWarnJob(IProjectRepository projectRepository, ILogger<ProjectPerformCloseJob> logger, IMasterEmailService masterEmailService) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var legacyProjects = await projectRepository.GetActiveProjectsWithGroupClaims();

        foreach (var legacyProject in legacyProjects)
        {
            var proj = await projectRepository.GetProjectAsync(legacyProject.ProjectId);

            if (proj.CreatedDate.Day != today.Day)
            {
                logger.LogInformation("Project {project} is using group claims. It will be warned on {dayOfMonth} day of month", legacyProject.ProjectId, proj.CreatedDate.Day);
                continue;
            }

            var email = new ProjectNotUsingSlots()
            {
                ProjectId = new(legacyProject.ProjectId),
            };

            await masterEmailService.EmalProjectNotUsingSlots(email);
        }

    }
}
