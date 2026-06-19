using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectCommon.Fields;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/project-fields/[action]")]
[RequireMaster]
public class ProjectFieldsController(IProjectFieldsClient client) : ControllerBase
{
    [HttpGet]
    public async Task<List<ProjectFieldDto>> GetProjectFields(ProjectIdentification projectId)
        => await client.GetProjectFields(projectId);
}
