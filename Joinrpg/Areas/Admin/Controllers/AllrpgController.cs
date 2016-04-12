using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
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

    // GET: Admin/Joinrpg
    public async Task<ActionResult> Index()
    {
      return View(new AllrpgIndexViewModel
      {
        Projects = (await ProjectRepository.GetActiveProjectsWithoutAllrpg())
      });
    }


    public async Task<ActionResult> AssociateProject(AssociateAllrpgProjectViewModel model)
    {
      try
      {
        await _allrpgService.AssociateProject(CurrentUserId, model.ProjectId, model.AllrpgProjectId);
        return await RedirectToProject(model.ProjectId);
      }
      catch (Exception)
      {
        return await Index();
      }
    }
  }
}