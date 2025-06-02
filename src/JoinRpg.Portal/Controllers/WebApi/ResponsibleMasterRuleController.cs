using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/resp-master-rule/[action]")]
[RequireMaster]
[IgnoreAntiforgeryToken]
public class ResponsibleMasterRuleController(IResponsibleMasterRuleClient responsibleMasterRuleClient) : ControllerBase
{
    [HttpGet]
    public async Task<ResponsibleMasterRuleListViewModel> GetList(int projectId)
        => await responsibleMasterRuleClient.GetResponsibleMasterRuleList(new ProjectIdentification(projectId));

    [HttpPost]
    [RequireMaster(Permission.CanManageClaims)]
    public async Task<ActionResult> Remove(int projectId, int ruleId)
    {
        await responsibleMasterRuleClient.RemoveResponsibleMasterRule(new ProjectIdentification(projectId), ruleId);
        return Ok();
    }

    [HttpPost]
    [RequireMaster(Permission.CanManageClaims)]
    public async Task<ResponsibleMasterRuleListViewModel> Add(
        int projectId,
        int groupId,
        int masterId)
    {
        return await responsibleMasterRuleClient.AddResponsibleMasterRule(
                new ProjectIdentification(projectId),
                groupId,
                masterId);
    }
}
