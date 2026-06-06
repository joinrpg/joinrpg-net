using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.Apis;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/tools/[action]")]
public class GameToolsController(IProjectMetadataRepository projectMetadataRepository) : JoinControllerGameBase
{
    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> Apis(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        return View(new ApisIndexViewModel(projectInfo));
    }
}
