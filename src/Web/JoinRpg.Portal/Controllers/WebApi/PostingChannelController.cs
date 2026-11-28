using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.AdminTools.Advertisement;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/PostingChannel/[action]")]
[AdminAuthorize]
public class PostingChannelController(IPostingChannelClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AvailableChatsResponse>> GetAvailableChats()
        => Ok(await client.GetAvailableChats());

    [HttpPost]
    public async Task<ActionResult> CreateChannel([FromQuery] TelegramChatId chatId, [FromQuery] string name)
    {
        await client.CreateChannel(new CreateChannelRequest(chatId, name));
        return Ok();
    }
}
