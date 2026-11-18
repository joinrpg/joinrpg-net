using JoinRpg.Web.Games.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/project-create/[action]")]
[Authorize]
[IgnoreAntiforgeryToken]
public class ProjectCreateController(IProjectCreateClient client) : ControllerBase
{
    [HttpGet]
    public Task<bool> IsProductionEnvironment() => client.IsProductionEnvironment();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjectCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        return Ok(await client.CreateProject(model));
    }
}
