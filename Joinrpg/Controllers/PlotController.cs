using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class PlotController : ControllerGameBase
  {

    private readonly IPlotService _plotService;

    public async Task<ActionResult> Index(int projectId)
    {
      return await PlotList(projectId, pf => true);
    }

    public async Task<ActionResult> InWork(int projectId)
    {
      return await PlotList(projectId, pf => pf.InWork);
    }

    public async Task<ActionResult> Ready(int projectId)
    {
      return await PlotList(projectId, pf => pf.Completed);
    }

    public PlotController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IPlotService plotService) : base(userManager, projectRepository, projectService)
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(AddPlotFolderViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var errorResult = AsMaster(project, acl => true);
      if (errorResult != null)
      {
        return errorResult;
      }

      try
      {
        await _plotService.CreatePlotFolder(project.ProjectId, viewModel.PlotFolderMasterTitle, viewModel.TodoField);
        return RedirectToAction("Index", "Plot", new {project.ProjectId});
      }
      catch (Exception)
      {
        return View(viewModel);
      }

    }

    [HttpGet]
    public async Task<ActionResult> Edit(int projectId, int plotFolderId)
    {
      var folder = await ProjectRepository.GetPlotFolderAsync(projectId, plotFolderId);
      return AsMaster(folder) ?? View(EditPlotFolderViewModel.FromFolder(folder));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(EditPlotFolderViewModel viewModel)
    {
      return await WithPlotFolderAsync(viewModel.ProjectId, viewModel.PlotFolderId, async folder =>
      {
        try
        {
          await
            _plotService.EditPlotFolder(viewModel.ProjectId, viewModel.PlotFolderId, viewModel.PlotFolderMasterTitle,
              viewModel.TodoField);
          return RedirectToAction("Index", "Plot", new {folder.ProjectId});
        }
        catch (Exception)
        {
          return View(viewModel);
        }
      });
    }

    [HttpGet]
    public async Task<ActionResult> CreateElement(int projectId, int plotFolderId)
    {
      return await WithPlotFolderAsync(projectId, plotFolderId, folder => View(new AddPlotElementViewModel()
      {
        ProjectId = projectId,
        PlotFolderId = plotFolderId,
        PlotFolderName = folder.MasterTitle,
        Data = CharacterGroupListViewModel.FromGroupAsMaster(folder.Project.RootGroup)
      }));
    }

    public async Task<ActionResult> CreateElement(int projectId, int plotFolderId, string content, string todoField,
      FormCollection other)
    {
      return await WithPlotFolderAsync(projectId, plotFolderId, async folder =>
      {
        try
        {
          var dict = other.ToDictionary();
          var targetGroups = GetDynamicCheckBoxesFromPost(dict, "group_");
          var targetChars = GetDynamicCheckBoxesFromPost(dict, "char_");
          await
            _plotService.AddPlotElement(projectId, plotFolderId, content, todoField, targetGroups, targetChars);
          return RedirectToAction("Index", "Plot", new {folder.ProjectId});
        }
        catch (Exception)
        {
          return View(new AddPlotElementViewModel()
          {
            ProjectId = projectId,
            PlotFolderId = plotFolderId,
            PlotFolderName = folder.MasterTitle,
            Data = CharacterGroupListViewModel.FromGroupAsMaster(folder.Project.RootGroup),
            Content = new MarkdownString(content),
            TodoField = todoField
          });
        }
      });
    }

    #region private methods

    private async Task<ActionResult> WithPlotFolderAsync(int projectId, int plotFolderId,
      Func<PlotFolder, Task<ActionResult>> action)
    {
      PlotFolder entity = await ProjectRepository.GetPlotFolderAsync(projectId, plotFolderId);
      return AsMaster(entity) ?? await action(entity);
    }

    private async Task<ActionResult> WithPlotFolderAsync(int projectId, int plotFolderId,
      Func<PlotFolder, ActionResult> action)
    {
      PlotFolder folder = await ProjectRepository.GetPlotFolderAsync(projectId, plotFolderId);
      return AsMaster(folder) ?? action(folder);
    }

    private async Task<ActionResult> PlotList(int projectId, Func<PlotFolder, bool> predicate)
    {
      var allFolders = (await ProjectRepository.GetPlots(projectId));
      var folders = allFolders.Where(predicate).ToList(); //Sadly, we have to do this, as we can't query using complex properties
      var project = await GetProjectFromList(projectId, folders);
      return AsMaster(project) ?? View("Index", PlotFolderListViewModel.FromProject(folders, project));
    }

    #endregion

    [HttpGet]
    public async Task<ActionResult> Delete(int projectId, int plotFolderId)
    {
      PlotFolder folder = await ProjectRepository.GetPlotFolderAsync(projectId, plotFolderId);
      var error = AsMaster(folder);
      if (error != null) return null;
      return View(EditPlotFolderViewModel.FromFolder(folder));
    }

    [HttpPost]
    public async Task<ActionResult> Delete(int projectId, int plotFolderId, FormCollection collection)
    {
      try
      {
        await _plotService.DeleteFolder(projectId, plotFolderId, CurrentUserId);
        return RedirectToAction("Index", new {projectId});
      }
      catch (Exception)
      {
        return await Delete(projectId, plotFolderId);
      }
    }
  }
}