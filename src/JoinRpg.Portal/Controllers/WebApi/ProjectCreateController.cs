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
            var project = await createProjectService.CreateProject(new CreateProjectRequest(new ProjectName(model.ProjectName), (ProjectTypeDto)model.ProjectType));

            return Ok(project.Value);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error creating project");
            return StatusCode(500);
        }
    }
}
