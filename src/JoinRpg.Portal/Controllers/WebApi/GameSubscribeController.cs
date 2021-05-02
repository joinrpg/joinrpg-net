using System.Threading.Tasks;
using JoinRpg.Web.GameSubscribe;
using JoinRpg.WebPortal.Managers.Subscribe;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi
{
    public class GameSubscribeController : ControllerBase
    {
        private readonly SubscribeViewService subscribeViewService;

        public GameSubscribeController(SubscribeViewService subscribeViewService)
        {
            this.subscribeViewService = subscribeViewService;
        }

        public async Task<SubscribeListViewModel> Index(int projectId, int masterId)
        {
            return await subscribeViewService.GetSubscribeForMaster(projectId, masterId);
        }
    }
}
