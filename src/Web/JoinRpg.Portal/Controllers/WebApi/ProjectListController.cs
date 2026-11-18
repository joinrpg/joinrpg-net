using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.AdminTools;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/projects/[action]")]
[Authorize]
public class ProjectListController(IProjectListClient client, IProjectListForAdminClient adminClient) : ControllerBase
{
    [HttpGet]
    public async Task<List<ProjectLinkViewModel>> GetProjects(ProjectSelectionCriteria criteria) => await client.GetProjects(criteria);

    [HttpGet]
    [AdminAuthorize]
    public async Task<List<ProjectAdminListItemViewModel>> GetProjectsForAdmin(ProjectSelectionCriteria criteria) => await adminClient.GetProjectsForAdmin(criteria);
}
