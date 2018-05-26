using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Domain;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
    public class UserController : Common.ControllerBase
    {
      [HttpGet]
      public async Task<ActionResult> Details(int userId)
      {
        var user = await UserManager.FindByIdAsync(userId);
        var currentUser = User.Identity.IsAuthenticated ? await GetCurrentUserAsync() : null;

        var accessReason = (AccessReason) user.GetProfileAccess(currentUser);
        var userProfileViewModel = new UserProfileViewModel()
        {
          DisplayName = user.GetDisplayName(),
          ThisUserProjects = user.ProjectAcls,
          UserId = user.UserId,
          Details = new  UserProfileDetailsViewModel(user, accessReason),
          HasAdminAccess = currentUser?.Auth?.IsAdmin ?? false,
          IsAdmin = user.Auth?.IsAdmin ?? false,
        };


        if (currentUser != null)
        {
          userProfileViewModel.CanGrantAccessProjects = currentUser.GetProjects(acl => acl.CanGrantRights);
          userProfileViewModel.Claims = new ClaimListViewModel(currentUser.UserId,
            user.Claims.Where(claim => claim.HasAccess(currentUser.UserId, ExtraAccessReason.Player)).ToArray(), 
            null, 
            showCount: false,
            showUserColumn: false);
        }

        return View(userProfileViewModel);
      }

      public UserController(ApplicationUserManager userManager)
        : base(userManager)
      {
      }

      [HttpGet,Authorize]
      public ActionResult Me()
      {
        return RedirectToAction("Details", new {UserId = CurrentUserId});
      }
    }
}
