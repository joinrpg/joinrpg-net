using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.Acl;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Профиль мастера (ADR019, §4). Любой мастер проекта — свой профиль, с правом выдавать доступ — чужой:
/// это проверяет клиент/сервис, атрибут лишь пускает мастеров.
/// </summary>
[Route("/webapi/{projectId}/master-profile/[action]")]
[IgnoreAntiforgeryToken]
[RequireMaster]
[ApiController]
public class MasterProfileController(IMasterProfileClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MasterProfileViewModel>> GetProfile(ProjectIdentification projectId, [FromQuery] UserIdentification userId)
        => Ok(await client.GetProfile(projectId, userId));

    [HttpPost]
    public async Task<ActionResult> SaveProfile(ProjectIdentification projectId, MasterProfileViewModel model)
    {
        model.ProjectId = projectId;
        await client.SaveProfile(model);
        return Ok();
    }
}
