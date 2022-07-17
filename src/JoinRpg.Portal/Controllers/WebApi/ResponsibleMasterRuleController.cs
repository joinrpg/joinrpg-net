using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/resp-master-rule/[action]")]
public class ResponsibleMasterRuleController : ControllerBase
{
    private readonly IResponsibleMasterRuleClient responsibleMasterRuleClient;

    public ResponsibleMasterRuleController(IResponsibleMasterRuleClient responsibleMasterRuleClient)
        => this.responsibleMasterRuleClient = responsibleMasterRuleClient;

    [HttpGet]
    public async Task<ResponsibleMasterRuleListViewModel> GetList(int projectId)
        => await responsibleMasterRuleClient.GetResponsibleMasterRuleList(new ProjectIdentification(projectId));

    [HttpPost]
    public async Task<ActionResult> Remove(int projectId, int ruleId)
    {
        await responsibleMasterRuleClient.RemoveResponsibleMasterRule(new ProjectIdentification(projectId), ruleId);
        return Ok();
    }

    [HttpPost]
    public async Task Add(int projectId, int groupId, int masterId)
        => await responsibleMasterRuleClient.AddResponsibleMasterRule(
            new ProjectIdentification(projectId),
            groupId,
            masterId);
}
