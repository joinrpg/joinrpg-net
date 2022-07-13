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
        => await responsibleMasterRuleClient.GetResponsibleMasterRuleList(new PrimitiveTypes.ProjectIdentification(projectId));

    [HttpPost]
    public async Task<ActionResult> Remove(int projectId, int ruleId)
    {
        await responsibleMasterRuleClient.RemoveResponsibleMasterRule(new PrimitiveTypes.ProjectIdentification(projectId), ruleId);
        return Ok();
    }
}
