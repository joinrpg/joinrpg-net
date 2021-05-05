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
        [AllowAnonymous]
        public async Task<ActionResult> Details(int userId)
        {
            var user = await UserRepository.GetById(userId);

            var currentUser = User.Identity?.IsAuthenticated == true ? await UserRepository.GetById(CurrentUserAccessor.UserId) : null;

            var accessReason = (AccessReason)user.GetProfileAccess(currentUser);
            var userProfileViewModel = new UserProfileViewModel()
            {
                DisplayName = user.GetDisplayName(),
                ThisUserProjects = user.ProjectAcls.Select(p => p.Project).ToLinkViewModels().ToList(),
                UserId = user.UserId,
                Details = new UserProfileDetailsViewModel(user, accessReason),
                HasAdminAccess = CurrentUserAccessor.IsAdmin,
                IsAdmin = user.Auth.IsAdmin,
            };

            if (currentUser != null)
            {
                userProfileViewModel.CanGrantAccessProjects =
                    currentUser.GetProjects(acl => acl.CanGrantRights).Where(project => project.Active).ToLinkViewModels().ToList();
                var claims = CurrentUserAccessor.IsAdmin
                    ? user.Claims.ToArray()
                    : user.Claims.Where(claim => claim.HasAccess(CurrentUserAccessor.UserId, ExtraAccessReason.Player)).ToArray();
                userProfileViewModel.Claims = new ClaimListViewModel(CurrentUserAccessor.UserId,
                    claims,
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
        public ActionResult Me() => RedirectToAction("Details", new { CurrentUserAccessor.UserId });
    }
}
