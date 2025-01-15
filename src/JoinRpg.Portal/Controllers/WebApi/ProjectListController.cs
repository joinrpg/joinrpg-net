using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;
[Route("/webapi/projects/[action]")]
[RequireMaster]
public class ProjectListController(IProjectListClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ProjectDto[]> GetProjectsWithMyMasterAccess() => await client.GetProjectsWithMyMasterAccess();
}
