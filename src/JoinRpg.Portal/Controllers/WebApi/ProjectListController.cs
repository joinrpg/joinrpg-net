using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;
[Route("/webapi/projects/[action]")]
[Authorize]
public class ProjectListController(IProjectListClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ProjectLinkViewModel[]> GetProjects(ProjectSelectionCriteria criteria) => await client.GetProjects(criteria);
}
