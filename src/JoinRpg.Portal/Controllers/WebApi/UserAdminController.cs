using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.AdminTools;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/UserAdmin/[action]")]
[AdminAuthorize]
public class UserAdminController(IUserAdminClient userAdminClient) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserAdminPanelViewModel>> GetAdminPanel([FromQuery] UserIdentification userId)
        => Ok(await userAdminClient.GetAdminPanel(userId));

    [HttpPost]
    public async Task<ActionResult> RemoveVkLink([FromQuery] UserIdentification userId)
    {
        await userAdminClient.RemoveVkLink(userId);
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> SetAdminFlag([FromQuery] UserIdentification userId, [FromQuery] bool value)
    {
        await userAdminClient.SetAdminFlag(userId, value);
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> SetVerificationFlag([FromQuery] UserIdentification userId, [FromQuery] bool value)
    {
        await userAdminClient.SetVerificationFlag(userId, value);
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> ChangeEmail([FromQuery] UserIdentification userId, [FromQuery] string newEmail)
    {
        await userAdminClient.ChangeEmail(userId, newEmail);
        return Ok();
    }
}
