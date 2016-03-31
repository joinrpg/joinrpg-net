using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Allrpg;
using JoinRpg.Web.Areas.Admin.Models;

namespace JoinRpg.Web.Areas.Admin.Controllers
{
  [Authorize]
  public class AllrpgController : Web.Controllers.Common.ControllerGameBase
  {
    #region services & repositories

    private readonly IAllrpgService _allrpgService;
    #endregion

    #region constructor

    public AllrpgController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IAllrpgService allrpgService, IExportDataService exportDataService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      _allrpgService = allrpgService;
    }

    #endregion

    [Authorize]
    // GET: Admin/Joinrpg
    public async Task<ActionResult> Index()
    {
      var user = await GetCurrentUserAsync();
      if (!user.Auth.IsAdmin)
      {
        throw new JoinRpgInvalidUserException();
      }
      return ShowIndex(user);
    }

    private ActionResult ShowIndex(User user)
    {
      return View(new AllrpgIndexViewModel
      {
        Projects =
          user.GetProjects(acl => acl.IsOwner || user.Auth.IsAdmin).Where(p => p.Active && p.Details?.AllrpgId == null)
      });
    }


    public async Task<ActionResult> AssociateProject(AssociateAllrpgProjectViewModel model)
    {
      var user = await GetCurrentUserAsync();
      user.RequestAdminAccess();
      try
      {
        await _allrpgService.AssociateProject(CurrentUserId, model.ProjectId, model.AllrpgProjectId);
        return await RedirectToProject(model.ProjectId);
      }
      catch (Exception)
      {
        return ShowIndex(user);
      }
    }
  }
}