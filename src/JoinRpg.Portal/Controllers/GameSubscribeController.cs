using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Subscribe;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [RequireMaster]
    [Route("{projectId}/subscribe/[action]")]
    public class GameSubscribeController : Common.ControllerGameBase
    {
        private readonly IUserSubscribeRepository userSubscribeRepository;
        private readonly IUserRepository userRepository;
        private readonly IUriService uriService;

        public GameSubscribeController(
            IUserSubscribeRepository userSubscribeRepository,
            IUserRepository userRepository,
            IUriService uriService,
            IProjectRepository projectRepository,
            IProjectService projectService)
            : base(projectRepository, projectService, userRepository)
        {
            this.userSubscribeRepository = userSubscribeRepository;
            this.userRepository = userRepository;
            this.uriService = uriService;
        }

        [HttpGet("{masterId}")]
        public async Task<ActionResult> ByMaster(int projectId, int masterId)
        {
            var data = await userSubscribeRepository.LoadSubscriptionsForProject(masterId, projectId);
            var currentUser = await userRepository.GetById(User.GetUserIdOrDefault().Value);

            return View(data.ToSubscribeListViewModel(currentUser, uriService));
        }
    }
}
