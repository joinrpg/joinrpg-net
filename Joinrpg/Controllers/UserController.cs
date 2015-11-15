using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Domain;
using JoinRpg.Web.Models;

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
          AllrpgId = user.Allrpg?.Sid
        };

        var currentUser = User.Identity.IsAuthenticated ? await GetCurrentUserAsync() : null;
        if (currentUser != null)
        {
          userProfileViewModel.CanGrantAccessProjects = currentUser.GetProjects(acl => acl.CanGrantRights);
        }
        return View(userProfileViewModel);
      }

      public UserController(ApplicationUserManager userManager) : base(userManager)
      {
      }
    }
}