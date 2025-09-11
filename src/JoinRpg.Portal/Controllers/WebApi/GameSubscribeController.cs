using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.Subscribe;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/gamesubscribe/[action]")]
[RequireMaster]
[IgnoreAntiforgeryToken]
public class GameSubscribeController(IGameSubscribeClient subscribeViewService) : ControllerBase
{
    [HttpGet]
    public async Task<SubscribeListViewModel> GetForMaster(int projectId, int masterId)
    {
        return await subscribeViewService.GetSubscribeForMaster(projectId, masterId);
    }

    [HttpPost]
    public async Task Unsubscribe(int projectId, int userSubscriptionsId)
        => await subscribeViewService.RemoveSubscription(projectId, userSubscriptionsId);

    [HttpPost]
    public async Task Save(int projectId, [FromBody] EditSubscribeViewModel model) => await subscribeViewService.SaveGroupSubscription(projectId, model);
}
