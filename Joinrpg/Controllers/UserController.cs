using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Domain;
using JoinRpg.Helpers.Web;
using JoinRpg.Web.Models;
using Microsoft.AspNet.Identity;

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
          DisplayName = user.DisplayName,
          ThisUserProjects = user.ProjectAcls,
          UserId = user.UserId,
          Details = new  UserProfileDetailsViewModel(user, accessReason),
          HasAdminAccess = currentUser?.Auth?.IsAdmin ?? false,
          IsAdmin = user.Auth?.IsAdmin ?? false
        };


        if (currentUser != null)
        {
          userProfileViewModel.CanGrantAccessProjects = currentUser.GetProjects(acl => acl.CanGrantRights);
          userProfileViewModel.Claims = new ClaimListViewModel(currentUser.UserId,
            user.Claims.Where(claim => claim.HasAnyAccess(currentUser.UserId)).ToArray(), 
            null, 
            showCount: false,
            showUserColumn: false);
        }

        return View(userProfileViewModel);
      }


      [Authorize]
      // GET: User preferred name. I.e. to display in page header.
      public ActionResult PreferredName()
      {
        var userId = CurrentUserId;
        var user =   UserManager.FindById(userId);

        var userProfileViewModel = new UserProfileViewModel()
        {
          DisplayName = user.DisplayName,
          ThisUserProjects = user.ProjectAcls,
          UserId = user.UserId,
          Hash = user.Email.GravatarHash()
        };

        return PartialView(userProfileViewModel);
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

      public ActionResult GetAvatar(int userId)
      {
        var hash = UserManager.FindById(userId).Email.GravatarHash();
        return Content($"https://www.gravatar.com/avatar/{hash}?d=identicon&s=64");
      }
    }
}
