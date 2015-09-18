using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class AclController : ControllerGameBase
  {
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> Add(AclViewModel viewModel)
    {
      var project1 = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var error = AsMaster(project1, acl => acl.CanGrantRights);
      if (error != null) return error;

      var user = UserManager.FindById(viewModel.UserId);
      try
      {
        ProjectService.GrantAccess(viewModel.ProjectId, viewModel.UserId, viewModel.CanGrantRights,
          viewModel.CanChangeFields, viewModel.CanChangeProjectProperties);
      }
      catch
      {
        return RedirectToAction("Details", "User", new {user.Id});
      }
      return RedirectToAction("Details", "Game", new {viewModel.ProjectId});
    }

    public AclController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService) : base(userManager, projectRepository, projectService)
    {
    }

    public async Task<ActionResult> Index(int projectId)
    {
      var project1 = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
      var error = AsMaster(project1, acl => acl.CanGrantRights);
      if (error != null)
        return error;

      return View(project1.ProjectAcls.Select(acl => new AclViewModel()
      {
        ProjectId = acl.ProjectId,
        ProjectAclId = acl.ProjectAclId,
        UserId = acl.UserId,
        CanApproveClaims = acl.CanApproveClaims,
        CanChangeFields = acl.CanChangeFields,
        CanChangeProjectProperties = acl.CanChangeProjectProperties,
        CanGrantRights = acl.CanGrantRights,
        Master = acl.User
      }));
    }
  }
}