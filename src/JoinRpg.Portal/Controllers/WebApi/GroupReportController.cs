using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.CharacterGroups.GroupReport;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/group-report/[action]")]
[RequireMaster]
public class GroupReportController(IGroupReportClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GroupReportViewModel>> Get([FromQuery] CharacterGroupIdentification characterGroupId)
    {
        var result = await client.GetReport(characterGroupId);
        return result is null ? NotFound() : Ok(result);
    }
}
