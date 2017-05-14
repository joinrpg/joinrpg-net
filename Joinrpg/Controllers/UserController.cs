using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Helpers.Web;
using JoinRpg.Web.Models;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Controllers
{
    public class UserController : Common.ControllerBase
    {
      [ProvidesContext]
      private IProjectRepository ProjectRepository { get; }

      [HttpGet]
      public async Task<ActionResult> Details(int userId)
      {
        var user = await UserManager.FindByIdAsync(userId);
        var currentUser = User.Identity.IsAuthenticated ? await GetCurrentUserAsync() : null;

        var userProfileViewModel = new UserProfileViewModel()
        {
          DisplayName = user.DisplayName,
          ThisUserProjects = user.ProjectAcls,
          UserId = user.UserId,
          Reason = currentUser != null
          ? (AccessReason)user.GetProfileAccess(currentUser)
          : AccessReason.NoAccess,
          Details = new  UserProfileDetailsViewModel(user),
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

        if (currentUser == user && user.Auth?.IsAdmin == true)
        {
          userProfileViewModel.CanGrantAccessProjects =
            userProfileViewModel.CanGrantAccessProjects.Union(await ProjectRepository.GetActiveProjectsWithClaimCount()).ToList();
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

      public UserController(ApplicationUserManager userManager, IProjectRepository projectRepository)
        : base(userManager)
      {
        ProjectRepository = projectRepository;
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