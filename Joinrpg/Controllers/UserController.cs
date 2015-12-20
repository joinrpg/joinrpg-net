using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Web.Models;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Controllers
{
    public class UserController : Common.ControllerBase
    {

      [HttpGet]
      // GET: User
      public async Task<ActionResult> Details(int userId)
      {
        var user = await UserManager.FindByIdAsync(userId);
        var currentUser = User.Identity.IsAuthenticated ? await GetCurrentUserAsync() : null;

        var userProfileViewModel = new UserProfileViewModel()
        {
          DisplayName = user.DisplayName,
          ThisUserProjects = user.ProjectAcls,
          UserId = user.UserId,

          Details = UserProfileDetailsViewModel.FromUser(user, currentUser)
        };


        if (currentUser != null)
        {
          userProfileViewModel.CanGrantAccessProjects = currentUser.GetProjects(acl => acl.CanGrantRights);
          userProfileViewModel.Claims =
            user.Claims.Where(claim => claim.HasAccess(currentUser.UserId))
              .Select(ClaimListItemViewModel.FromClaim);

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
          UserId = user.UserId
        };

        return PartialView(userProfileViewModel);
      }

        public UserController(ApplicationUserManager userManager) : base(userManager)
      {
      }

      [HttpGet,Authorize]
      public ActionResult Me()
      {
        return RedirectToAction("Details", new {UserId = CurrentUserId});
      }

      public ActionResult GetAvatar(int userId)
      {
      var user = UserManager.FindById(userId);
        var hash = user.Email.ToLowerInvariant().ToHexHash(MD5.Create());
      return Content($"https://www.gravatar.com/avatar/{hash}?d=retro&s=64");
    }
    }
}