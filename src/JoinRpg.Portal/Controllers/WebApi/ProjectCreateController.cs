using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;
[Route("/webapi/project/[action]")]
[Authorize]
public class ProjectCreateController(ILogger<ProjectCreateController> logger) : ControllerBase
{
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create([FromBody] ProjectCreateViewModel model, [FromServices] ICreateProjectService createProjectService)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        try
        {
            var request = CreateProjectRequest.Create(new ProjectName(model.ProjectName),
                (ProjectTypeDto)model.ProjectType,
                model.CopyFromProjectId,
                (ProjectCopySettingsDto)model.CopySettings
                );
            var project = await createProjectService.CreateProject(request);

            return Ok(project.Value);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error creating project");
            return Problem(title: "Произошла ошибка при обработке запроса", detail: exception.Message, statusCode: 500);
        }
    }
}
