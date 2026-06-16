using JoinRpg.Web.Claims;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/ClaimOperations/[action]")]
public class ClaimOperationsController(IClaimOperationClient claimClient) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> AllowSensitiveData([FromQuery] ProjectIdentification projectId)
    {
        await claimClient.AllowSensitiveData(projectId);
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> AcceptInvitation([FromQuery] ClaimIdentification claimId, [FromBody] AcceptInvitationRequest request)
    {
        await claimClient.AcceptInvitation(claimId, request);
        return Ok();
    }
}
