using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectCommon.ElementMoving;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

public sealed record MoveAfterBody(string SelfId, string ParentId, string? MoveAfterId);

[Route("/webapi/move/[action]")]
[IgnoreAntiforgeryToken]
[RequireMaster]
public class MoveController(IMoveClient moveClient) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<string[]>> MoveAfter([FromBody] MoveAfterBody body)
    {
        if (!MoveRequest.TryParse(body.SelfId, body.ParentId, body.MoveAfterId, out var request))
            return BadRequest($"Cannot parse IDs: selfId='{body.SelfId}', parentId='{body.ParentId}', moveAfterId='{body.MoveAfterId}'");

        return Ok(await moveClient.MoveAfterAsync(request));
    }
}
