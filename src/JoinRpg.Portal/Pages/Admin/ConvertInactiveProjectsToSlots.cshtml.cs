using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

[AdminAuthorize]

public class ConvertInactiveProjectsToSlotsModel(
    IProjectRepository projectRepository,
    ISlotMassConvertService slotMassConvertService,
    ILogger<ConvertInactiveProjectsToSlotsModel> logger
    ) : PageModel
{
    public async Task OnGet()
    {
        var projects = await projectRepository.GetInactiveProjectsWithSlots();

        logger.LogInformation("Found {count} projects to convert", projects.Length);

        foreach (var projectId in projects)
        {
            var id = new PrimitiveTypes.ProjectIdentification(projectId);
            logger.LogInformation("About to convert {projectId} to slots", id);
            await slotMassConvertService.MassConvert(id);
            logger.LogInformation("Converted {projectId} to slots", id);
        }

    }
}
