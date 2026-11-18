using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Portal.Infrastructure.XApi;
using JoinRpg.WebPortal.Managers.Characters;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-api/users/"), XAdminAuthorize()]
public class XUserInfoController(
    IUserRepository userRepository,
    IAvatarUriResolver avatarUriResolver
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
        // Аватарка есть у каждого пользователя (#5602); пустая строка — только если где-то её всё же не завели.
        var avatarUrl = userInfo.SelectedAvatar is { } avatar
            ? avatarUriResolver.GetAvatarUri(avatar.CachedUri, avatar.OriginalUri).AbsoluteUri
            : "";
        return Ok(
            new PlayerInfo(userInfo.UserId, userInfo.DisplayName.DisplayName, userInfo.UserFullName.FullName, avatarUrl, ApiInfoBuilder.ToPlayerContacts(userInfo))
            );
    }


}
