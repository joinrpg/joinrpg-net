using Joinrpg.Web.Identity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.XApi;
using JoinRpg.Services.Interfaces.Avatars;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-api/users/"), XAdminAuthorize()]
public class XUserInfoController(
    IUserRepository userRepository,
    IAvatarLoader avatarLoader,
    IAvatarService avatarService
        ) : XGameApiController()
{

    [HttpGet]
    [Route("{userId:int}/")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<CheckinPlayerInfo>> GetUser(int userId)
    {
        var userInfo = await userRepository.GetUserInfo(new(userId));
        if (userInfo is null)
        {
            return new StatusCodeResult(410);
        }
        var userAvatarId = userInfo.SelectedAvatarId ?? await avatarService.EnsureAvatarPresent(userInfo.UserId);
        var avatarUrl = await avatarLoader.GetAvatar(userAvatarId, 64);
        return Ok(
            new PlayerInfo(userInfo.UserId, userInfo.DisplayName.DisplayName, userInfo.UserFullName.FullName, avatarUrl.Uri.AbsoluteUri, ApiInfoBuilder.ToPlayerContacts(userInfo))
            );
    }


}
