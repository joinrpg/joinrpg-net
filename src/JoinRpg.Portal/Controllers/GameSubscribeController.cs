using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Subscribe;
using JoinRpg.Web.ProjectMasterTools.Subscribe;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[TypeFilter<CaptureNoAccessExceptionFilter>]
[DiscoverProjectFilter]
[RequireMaster]
[Route("{projectId}/subscribe/[action]")]
public class GameSubscribeController(
    IUserSubscribeRepository userSubscribeRepository,
    IUserRepository userRepository,
    IUriService uriService,
    IGameSubscribeClient subscribeClient,
    ICurrentUserAccessor currentUserAccessor
        ) : Controller
{

#pragma warning disable ASP0023 // Route conflict detected between controller actions There is no one because of [action]
    [HttpGet("{masterId}")]
#pragma warning restore ASP0023 // Route conflict detected between controller actions
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

#pragma warning disable ASP0023 // Route conflict detected between controller actions. There is no one because of [action]
    [HttpGet("{characterGroupId}")]
#pragma warning restore ASP0023 // Route conflict detected between controller actions
    public ActionResult EditForGroup(int projectId, int characterGroupId)
    {
        return RedirectToAction(actionName: "ByMaster", routeValues: new { MasterId = currentUserAccessor.UserId, ProjectId = projectId });
    }

}
