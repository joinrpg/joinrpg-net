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
        => await subscribeViewService.GetSubscribeForMaster(projectId, masterId);

    [HttpPost]
    public async Task Unsubscribe(int projectId, int userSubscriptionsId)
        => await subscribeViewService.RemoveSubscription(projectId, userSubscriptionsId);

    [HttpPost]
    public async Task Save(int projectId, [FromBody] EditSubscribeViewModel model)
        => await subscribeViewService.SaveGroupSubscription(projectId, model);

    [HttpGet]
    public async Task<ClaimSubscribeViewModel> GetForClaim(ClaimIdentification claimId)
        => await subscribeViewService.GetSubscribeForClaim(claimId);

    [HttpPost]
    public async Task<ClaimSubscribeViewModel> SubscribeClaim(ClaimIdentification claimId)
        => await subscribeViewService.SubscribeClaimToUser(claimId);

    [HttpPost]
    public async Task<ClaimSubscribeViewModel> UnsubscribeClaim(ClaimIdentification claimId)
        => await subscribeViewService.UnsubscribeClaimToUser(claimId);
}
