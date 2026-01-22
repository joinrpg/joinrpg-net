using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.Games.Projects;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/{projectId}/project-info/[action]")]
[IgnoreAntiforgeryToken]
[ApiController]
public class ProjectInfoController : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<ProjectInfoViewModel>> GetProjectInfo(ProjectIdentification projectId, [FromServices] IProjectInfoClient client)
        => Ok(await client.GetProjectInfo(projectId));
}
