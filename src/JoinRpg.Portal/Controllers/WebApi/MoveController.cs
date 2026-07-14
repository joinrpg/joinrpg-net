using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.WebComponents;
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
        => Ok(await moveClient.MoveAfterAsync(body.SelfId, body.ParentId, body.MoveAfterId));
}
