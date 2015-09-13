using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class PlotController : ControllerGameBase
  {

    private readonly IPlotService _plotService;
    public async Task<ActionResult> Index(int projectId)
    {
      return await WithProjectAsMasterAsync(projectId, project => View(
        new PlotFolderListViewModel()
        {
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName,
          Folders = project.PlotFolders
        }));
    }

    public async Task<ActionResult> InWork(int projectId)
    {
      return await WithProjectAsMasterAsync(projectId, project => View("Index",
        new PlotFolderListViewModel()
        {
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName,
          Folders = project.PlotFolders.Where(pf => !pf.Completed())
        }));
    }

    public async Task<ActionResult> Ready(int projectId)
    {
      return await WithProjectAsMasterAsync(projectId, project => View("Index",
        new PlotFolderListViewModel()
        {
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName,
          Folders = project.PlotFolders.Where(pf => pf.Completed())
        }));
    }

    public PlotController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService, IPlotService plotService) : base(userManager, projectRepository, projectService)
    {
      _plotService = plotService;
    }

    [HttpGet]
    public async Task<ActionResult> Create(int projectId)
    {
      return await WithProjectAsMasterAsync(projectId, project => View(new AddPlotFolderViewModel
      {
        ProjectId = project.ProjectId,
        ProjectName = project.ProjectName
      }));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(AddPlotFolderViewModel viewModel)
    {
      return await WithProjectAsMasterAsync(viewModel.ProjectId, async project =>
      {
        try
        {
          await _plotService.CreatePlotFolder(project.ProjectId, viewModel.PlotFolderMasterTitle, viewModel.TodoField);
          return RedirectToAction("Index", "Plot", new {project.ProjectId});
        }
        catch (Exception)
        {
          return View(viewModel);
        }
      });
    }
  }
}