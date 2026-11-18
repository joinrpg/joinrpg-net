using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.Badges;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/badge-template/[action]")]
[RequireMaster]
[IgnoreAntiforgeryToken]
public class BadgeTemplateController(IBadgeTemplateClient client) : ControllerBase
{
    [HttpGet]
    public async Task<BadgeTemplateListViewModel> GetList(ProjectIdentification projectId) => await client.GetList(projectId);

    // Как и в остальных экшенах, шаблон собирается из projectId, по которому проверены права, и числа.
    [HttpGet]
    public async Task<EditBadgeTemplateViewModel> GetById([FromQuery] ProjectIdentification projectId, [FromQuery] int badgeTemplateId)
        => await client.GetById(new ProjectBadgeTemplateIdentification(projectId, badgeTemplateId));

    [HttpPost]
    [RequireMaster(Permission.CanChangeProjectProperties)]
    public async Task<ActionResult<ProjectBadgeTemplateIdentification>> Create([FromQuery] ProjectIdentification projectId, [FromBody] EditBadgeTemplateViewModel model)
        => Ok(await client.Create(projectId, model));

    [HttpPost]
    [RequireMaster(Permission.CanChangeProjectProperties)]
    public async Task<ActionResult> Update([FromQuery] ProjectIdentification projectId, [FromQuery] int badgeTemplateId, [FromBody] EditBadgeTemplateViewModel model)
    {
        await client.Update(new ProjectBadgeTemplateIdentification(projectId, badgeTemplateId), model);
        return Ok();
    }

    [HttpPost]
    [RequireMaster(Permission.CanChangeProjectProperties)]
    public async Task<ActionResult> Remove([FromQuery] ProjectIdentification projectId, [FromQuery] int badgeTemplateId)
    {
        await client.Remove(new ProjectBadgeTemplateIdentification(projectId, badgeTemplateId));
        return Ok();
    }

    [HttpPost]
    [RequireMaster(Permission.CanChangeProjectProperties)]
    public async Task<ActionResult> Restore([FromQuery] ProjectIdentification projectId, [FromQuery] int badgeTemplateId)
    {
        await client.Restore(new ProjectBadgeTemplateIdentification(projectId, badgeTemplateId));
        return Ok();
    }
}
