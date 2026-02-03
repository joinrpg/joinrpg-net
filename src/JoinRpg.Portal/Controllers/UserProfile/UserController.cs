using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Logging;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.ClaimList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

public class UserController(IUserRepository userRepository, ICurrentUserAccessor currentUserAccessor, YandexLogLink yandexLogLink,
    IProjectRepository projectRepository, IClaimsRepository claimsRepository) : Common.ControllerBase()
{
    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<ActionResult> Details(UserIdentification userId)
    {
        var user = await userRepository.GetUserInfo(userId) ?? throw new JoinRpgEntityNotFoundException([userId], "User");

        var userProjects = await projectRepository.GetPersonalizedProjectsBySpecification(userId, ProjectListSpecification.AllProjectsWithMasterAccess);

        var currentUser = User.Identity?.IsAuthenticated == true ? await userRepository.GetUserInfo(currentUserAccessor.UserIdentification) : null;

        var userProfileViewModel = new UserProfileViewModel()
        {
            DisplayName = user.DisplayName.DisplayName,
            ThisUserProjects = userProjects.ToLinkViewModels().ToList(),
            UserId = user.UserId,
            Details = new UserProfileDetailsViewModel(user, currentUser),
            IsAdmin = user.IsAdmin,
            Admin = currentUserAccessor.IsAdmin ? new UserAdminOperationsViewModel(yandexLogLink.GetLinkForUser(user.Email), user.IsAdmin) : null,
        };

        if (currentUser != null)
        {
            var canGrantProjects = await projectRepository.GetPersonalizedProjectsBySpecification(currentUser.UserId, ProjectListSpecification.ActiveProjectsWithGrantMasterAccess);

            userProfileViewModel.CanGrantAccessProjects = canGrantProjects.ToLinkViewModels().ToList();

            var claims = await claimsRepository.GetClaimsForPlayer(userId, ClaimStatusSpec.Any);

            userProfileViewModel.Claims = new MyClaimListViewModel(
                [.. claims.Where(claim => currentUserAccessor.IsAdmin || claim.PlayerUserId == currentUser.UserId || currentUser.AllProjects.Contains(new(claim.ProjectId)))]);
        }

        return View(userProfileViewModel);
    }

    [Authorize]
    [HttpGet("user/me")]
    public ActionResult Me() => RedirectToAction("Details", new { currentUserAccessor.UserId });
}
