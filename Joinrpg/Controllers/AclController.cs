using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
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
    public ActionResult Add(AclViewModel viewModel)
    {
      return WithProjectAsMaster(viewModel.ProjectId, acl => acl.CanGrantRights, project =>
      {
        var user = UserManager.FindById(viewModel.UserId);
        try
        {
          ProjectService.GrantAccess(viewModel.ProjectId, viewModel.UserId, viewModel.CanGrantRights,
            viewModel.CanChangeFields, viewModel.CanChangeProjectProperties);
        }
        catch
        {
          return RedirectToAction("Details", "User", new {user.Email});
        }
        return RedirectToAction("Details", "Game", new {viewModel.ProjectId});
      });
    }

    public AclController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService) : base(userManager, projectRepository, projectService)
    {
    }
  }
}