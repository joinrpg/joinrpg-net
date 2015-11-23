using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Domain;
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

        var userProfileViewModel = new UserProfileViewModel()
        {
          DisplayName = user.DisplayName,
          FullName = user.FullName,
          ThisUserProjects = user.ProjectAcls,
          UserId = user.UserId,
          AllrpgId = user.Allrpg?.Sid,
        };

        var currentUser = User.Identity.IsAuthenticated ? await GetCurrentUserAsync() : null;
        if (currentUser != null)
        {
          userProfileViewModel.CanGrantAccessProjects = currentUser.GetProjects(acl => acl.CanGrantRights);
          userProfileViewModel.Claims =
            user.Claims.Where(claim => claim.HasAccess(currentUser.UserId))
              .Select(ClaimListItemViewModel.FromClaim);
        }
        return View(userProfileViewModel);
      }

      [HttpGet]
      [Authorize]
      // GET: User
      public ActionResult PreferredName()
      {
        var userId = CurrentUserId;
        var user =   UserManager.FindById(userId);

        var userProfileViewModel = new UserProfileViewModel()
        {
          DisplayName = user.DisplayName,
          FullName = user.FullName,
          ThisUserProjects = user.ProjectAcls,
          UserId = user.UserId,
          AllrpgId = user.Allrpg?.Sid
        };

        return PartialView(userProfileViewModel);
      }

        public UserController(ApplicationUserManager userManager) : base(userManager)
      {
      }
    }
}