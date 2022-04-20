using JoinRpg.Web.GameSubscribe;
using JoinRpg.WebPortal.Managers.Subscribe;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi
{
    [Route("/webapi/gamesubscribe/[action]")]
    public class GameSubscribeController : ControllerBase
    {
        private readonly SubscribeViewService subscribeViewService;

        public GameSubscribeController(SubscribeViewService subscribeViewService)
        {
            this.subscribeViewService = subscribeViewService;
        }

        [HttpGet]
        public async Task<SubscribeListViewModel> GetForMaster(int projectId, int masterId)
        {
            return await subscribeViewService.GetSubscribeForMaster(projectId, masterId);
        }

        [HttpPost]
        public async Task Unsubscribe(int projectId, int userSubscriptionsId)
        {
            await subscribeViewService.RemoveSubscription(projectId, userSubscriptionsId);
        }
    }
}
