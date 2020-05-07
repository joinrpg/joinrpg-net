using System;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure;
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

        [HttpGet("{characterGroupId}")]
        public async Task<ActionResult> EditForGroup(int projectId, int characterGroupId)
        {
            var group = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
            if (group == null)
            {
                return NotFound();
            }

            var user = await UserRepository.GetWithSubscribe(CurrentUserId);

            return View(new SubscribeSettingsViewModel(user, group));
        }

        [HttpPost("{characterGroupId}")]
        public async Task<ActionResult> EditForGroup(SubscribeSettingsViewModel viewModel)
        {
            var group = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, viewModel.CharacterGroupId);

            if (group == null)
            {
                return NotFound();
            }

            var user = await UserRepository.GetWithSubscribe(CurrentUserId);

            var serverModel = new SubscribeSettingsViewModel(user, group);

            serverModel.Options.AssignFrom(viewModel.Options);

            try
            {
                await
                    ProjectService.UpdateSubscribeForGroup(new SubscribeForGroupRequest
                    {
                        CharacterGroupId = group.CharacterGroupId,
                        ProjectId = group.ProjectId,
                    }.AssignFrom(serverModel.GetOptionsToSubscribeDirectly()));

                return RedirectToIndex(group.Project);
            }
            catch (Exception e)
            {
                ModelState.AddException(e);
                return View(serverModel);
            }

        }

    }
}
