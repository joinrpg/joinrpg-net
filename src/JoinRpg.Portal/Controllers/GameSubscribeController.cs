using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Subscribe;
using JoinRpg.Web.ProjectMasterTools.Subscribe;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[RequireMaster]
[Route("{projectId}/subscribe/[action]")]
public class GameSubscribeController : Controller
{
    private readonly IUserSubscribeRepository userSubscribeRepository;
    private readonly IUserRepository userRepository;
    private readonly IUriService uriService;
    private readonly IGameSubscribeClient subscribeClient;
    private readonly ICurrentUserAccessor currentUserAccessor;

    public GameSubscribeController(
        IUserSubscribeRepository userSubscribeRepository,
        IUserRepository userRepository,
        IUriService uriService,
        IGameSubscribeClient subscribeClient,
        ICurrentUserAccessor currentUserAccessor
        )
    {
        this.userSubscribeRepository = userSubscribeRepository;
        this.userRepository = userRepository;
        this.uriService = uriService;
        this.subscribeClient = subscribeClient;
        this.currentUserAccessor = currentUserAccessor;
    }

    [HttpGet("{masterId}")]
    public async Task<ActionResult> ByMaster(int projectId, int masterId)
    {
        var subscribeViewModel = await subscribeClient.GetSubscribeForMaster(projectId, masterId);

        var user = await userRepository.GetById(masterId);
        var currentUser = await userRepository.GetById(currentUserAccessor.UserId);


        return View(
            new SubscribeByMasterPageViewModel(
                new UserProfileDetailsViewModel(user, currentUser),
                subscribeViewModel));
    }


    [HttpGet]
    public async Task<ActionResult> EditRedirect(int projectId, int subscriptionId)
    {
        var subscribe = await userSubscribeRepository.LoadSubscriptionById(projectId, subscriptionId);
        var link = subscribe.ToSubscribeTargetLink();
        return Redirect(uriService.GetUri(link).AbsoluteUri);
    }

    [HttpGet("{characterGroupId}")]
    public ActionResult EditForGroup(int projectId, int characterGroupId)
    {
        return RedirectToAction(actionName: "ByMaster", routeValues: new { MasterId = currentUserAccessor.UserId, ProjectId = projectId });
    }

}
