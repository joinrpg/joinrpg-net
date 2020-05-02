using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    public class UserController : Common.ControllerBase
    {
        public IUserRepository UserRepository { get; }
        public ICurrentUserAccessor CurrentUserAccessor { get; }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult> Details(int userId)
        {
            var user = await UserRepository.GetById(userId);

            var currentUser =  User.Identity.IsAuthenticated ? await UserRepository.GetById(CurrentUserAccessor.UserId) : null;

            var accessReason = (AccessReason) user.GetProfileAccess(currentUser);
            var userProfileViewModel = new UserProfileViewModel()
            {
                DisplayName = user.GetDisplayName(),
                ThisUserProjects = user.ProjectAcls,
                UserId = user.UserId,
                Details = new UserProfileDetailsViewModel(user, accessReason),
                HasAdminAccess = currentUser?.Auth.IsAdmin ?? false,
                IsAdmin = user.Auth.IsAdmin,
            };

            if (currentUser != null)
            {
                userProfileViewModel.CanGrantAccessProjects =
                    currentUser.GetProjects(acl => acl.CanGrantRights);
                userProfileViewModel.Claims = new ClaimListViewModel(currentUser.UserId,
                    user.Claims.Where(claim =>
                        claim.HasAccess(currentUser.UserId, ExtraAccessReason.Player)).ToArray(),
                    null,
                    showCount: false,
                    showUserColumn: false);
            }

            return View(userProfileViewModel);
        }

        public UserController(IUserRepository userRepository, ICurrentUserAccessor currentUserAccessor)
            : base()
        {
            UserRepository = userRepository;
            CurrentUserAccessor = currentUserAccessor;
        }

        [Authorize]
        [HttpGet("user/me")]
        public ActionResult Me()
        {
            return RedirectToAction("Details", new {UserId = CurrentUserAccessor.UserId });
        }
    }
}
