using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Helpers;
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

    [HttpGet]
    public async Task<ActionResult> Edit(int projectId, int plotFolderId)
    {
      return await WithPlotFolderAsync(projectId, plotFolderId, folder => View(new EditPlotFolderViewModel()
      {
        PlotFolderMasterTitle = folder.MasterTitle,
        PlotFolderId = folder.PlotFolderId,
        TodoField = folder.TodoField,
        ProjectId = folder.ProjectId,
        Elements = folder.Elements.Select(e => new PlotElementListItemViewModel()
        {
          PlotFolderElementId = e.PlotElementId,
          For = e.Targets.Select(t => t.AsObjectLink())
        })
      }));
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
            Content = content,
            TodoField = todoField
          });
        }
      });
    }

    #region private methods

    private async Task<ActionResult> WithPlotFolderAsync(int projectId, int plotFolderId,
      Func<PlotFolder, Task<ActionResult>> action)
    {
      return await AsMaster(await ProjectRepository.GetPlotFolderAsync(projectId, plotFolderId), action);
    }

    private async Task<ActionResult> WithPlotFolderAsync(int projectId, int plotFolderId,
      Func<PlotFolder, ActionResult> action)
    {
      return await AsMaster(await ProjectRepository.GetPlotFolderAsync(projectId, plotFolderId), action);
    }

    //TODO: This should use special ProjectRepository method 
    private async Task<ActionResult> PlotList(int projectId, Func<PlotFolder, bool> predicate)
    {
      return await WithProjectAsMasterAsync(projectId, project => View("Index",
        new PlotFolderListViewModel()
        {
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName,
          Folders = project.PlotFolders.Where(predicate)
        }));
    }
    #endregion
  }
}