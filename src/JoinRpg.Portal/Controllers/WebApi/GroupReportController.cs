using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.CharacterGroups.GroupReport;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/group-report/[action]")]
[RequireMaster]
public class GroupReportController(IGroupReportClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GroupReportViewModel>> Get(
        [FromQuery] ProjectIdentification projectId, [FromQuery] int characterGroupId)
    {
        var groupId = new CharacterGroupIdentification(projectId, characterGroupId);
        var result = await client.GetReport(groupId);
        return result is null ? NotFound() : Ok(result);
    }
}
