using System;
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
      var project1 = ProjectRepository.GetProject(viewModel.ProjectId);
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
  }
}