using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.WebPortal.Managers.Projects;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-game-api/{projectId}/metadata"), XGameMasterAuthorize]
public class MetaDataApiController(IProjectApiViewService projectApiViewService) : XGameApiController
{

    /// <summary>
    /// All required metadata for fields
    /// </summary>
    [HttpGet]
    [Route("fields")]
    public async Task<ProjectFieldsMetadata> GetFieldsList(int projectId)
    {
        return await projectApiViewService.GetFieldsMetadata(new ProjectIdentification(projectId));
    }
}
