using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Web.ProjectMasterTools.CaptainRules;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/captain-rule/[action]")]
[RequireMaster]
[IgnoreAntiforgeryToken]
public class CaptainRuleController(ICaptainRuleClient client) : ControllerBase
{
    [HttpGet]
    public async Task<CaptainRuleListViewModel> GetList(ProjectIdentification projectId)
        => await client.GetList(projectId);

    [HttpPost]
    [RequireMaster(Permission.CanManageClaims)]
    public async Task<ActionResult> Remove([FromQuery] ProjectIdentification projectId, [FromBody] CaptainAccessRule captainAccessRule)
    {
        if (captainAccessRule.ProjectId != projectId)
        {
            return BadRequest();
        }
        await client.Remove(captainAccessRule);
        return Ok();
    }

    [HttpPost]
    [RequireMaster(Permission.CanManageClaims)]
    public async Task<ActionResult<CaptainRuleListViewModel>> AddOrChange([FromQuery] ProjectIdentification projectId, [FromBody] CaptainAccessRule captainAccessRule)
    {
        if (captainAccessRule.ProjectId != projectId)
        {
            return BadRequest();
        }
        return Ok(await client.AddOrChange(captainAccessRule));
    }
}
