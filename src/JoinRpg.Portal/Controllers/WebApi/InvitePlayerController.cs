using JoinRpg.Helpers;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectCommon.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[ApiController]
[Route("/webapi/invite-player/[action]")]
[RequireMaster(Permission.CanManageClaims)]
[IgnoreAntiforgeryToken]
public class InvitePlayerController(IInvitePlayerClient client) : ControllerBase
{
    [HttpPost]
    public async Task<Results<Ok<ClaimIdentification>, BadRequest<string>>> Invite(ProjectIdentification projectId, CharacterIdentification targetId, string userLink, string claimText)
    {
        if (targetId.ProjectId != projectId)
        {
            return TypedResults.BadRequest("Персонаж не принадлежит проекту");
        }
        try
        {
            return TypedResults.Ok(await client.InvitePlayer(targetId, userLink, claimText));
        }
        catch (FormatException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (JoinRpgBaseException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }
}
