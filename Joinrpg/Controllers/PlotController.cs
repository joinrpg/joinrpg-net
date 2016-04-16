using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class PlotController : ControllerGameBase
  {
    private readonly IPlotService _plotService;
    private readonly IPlotRepository _plotRepository;

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
      IProjectService projectService, IPlotService plotService, IPlotRepository plotRepository,
      IExportDataService exportDataService) : base(userManager, projectRepository, projectService, exportDataService)
    {
      _plotService = plotService;
      _plotRepository = plotRepository;
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
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      return AsMaster(folder) ?? View(EditPlotFolderViewModel.FromFolder(folder));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(EditPlotFolderViewModel viewModel)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(viewModel.ProjectId, viewModel.PlotFolderId);
      var error = AsMaster(folder);
      if (error != null)
      {
        return error;
      }
      try
      {
        await
          _plotService.EditPlotFolder(viewModel.ProjectId, viewModel.PlotFolderId, viewModel.PlotFolderMasterTitle, viewModel.TodoField);
        return ReturnToPlot(viewModel.PlotFolderId, viewModel.ProjectId);
      }
      catch (Exception)
      {
        return View(viewModel);
      }
    }

    [HttpGet]
    public async Task<ActionResult> CreateElement(int projectId, int plotFolderId)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      return AsMaster(folder) ?? View(new AddPlotElementViewModel()
      {
        ProjectId = projectId,
        PlotFolderId = plotFolderId,
        PlotFolderName = folder.MasterTitle,
        RootGroupId = folder.Project.RootGroup.CharacterGroupId
      });
    }

    public async Task<ActionResult> CreateElement(int projectId, int plotFolderId, MarkdownViewModel content, string todoField, ICollection<string> targets)
    { 
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      var error = AsMaster(folder);
      if (error != null)
      {
        return error;
      }
      try
      {
        var targetGroups = targets.GetUnprefixedGroups();
        var targetChars = targets.GetUnprefixedChars();
        await
          _plotService.AddPlotElement(projectId, plotFolderId, content.Contents, todoField, targetGroups, targetChars);
        return ReturnToPlot(plotFolderId, projectId);
      }
      catch (Exception)
      {
        return View(new AddPlotElementViewModel()
        {
          ProjectId = projectId,
          PlotFolderId = plotFolderId,
          PlotFolderName = folder.MasterTitle,
          Content = content,
          TodoField = todoField,
          RootGroupId = folder.Project.RootGroup.CharacterGroupId
        });
      }
    }

    #region private methods

    private async Task<ActionResult> PlotList(int projectId, Func<PlotFolder, bool> predicate)
    {
      var allFolders = await _plotRepository.GetPlots(projectId);
      var folders = allFolders.Where(predicate).ToList(); //Sadly, we have to do this, as we can't query using complex properties
      var project = await GetProjectFromList(projectId, folders);
      return AsMaster(project) ?? View("Index", PlotFolderListViewModel.FromProject(folders, project));
    }

    #endregion

    [HttpGet]
    public async Task<ActionResult> Delete(int projectId, int plotFolderId)
    {
      PlotFolder folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
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

    [HttpPost]
    public async Task<ActionResult> DeleteElement(int plotelementid, int plotFolderId, int projectId)
    {
      try
      {
        await _plotService.DeleteElement(projectId, plotFolderId, plotelementid, CurrentUserId);
        return ReturnToPlot(plotFolderId, projectId);
      }
      catch (Exception)
      {
        return await Edit(projectId, plotFolderId);
      }
    }

    private ActionResult ReturnToPlot(int plotFolderId, int projectId)
    {
      return RedirectToAction("Edit", new {projectId, plotFolderId});
    }

    [HttpPost]
    public async Task<ActionResult> EditElement(int plotelementid, int plotFolderId, int projectId, MarkdownViewModel content, string todoField,
      bool isCompleted, ICollection<string> targets)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      var error = AsMaster(folder);
      if (error != null)
      {
        return error;
      }
      try
      {
        var targetGroups = targets.GetUnprefixedGroups();
        var targetChars = targets.GetUnprefixedChars();
        await
          _plotService.EditPlotElement(projectId, plotFolderId, plotelementid, content.Contents, todoField, targetGroups, targetChars, isCompleted, CurrentUserId);
        return ReturnToPlot(plotFolderId, projectId);
      }
      catch (Exception)
      {
        return await Edit(projectId, plotFolderId);
      }
    }


    private async Task<ActionResult> WithProjectAsMasterAsync(int projectId, Func<Project, ActionResult> action)
    {
      var project1 = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project1) ?? action(project1);
    }

    public Task<ActionResult> MoveUpElementForCharacter(int projectid, int plotelementid, int characterid)
    {
      return MoveElementImpl(projectid, plotelementid, characterid, -1);
    }

    private async Task<ActionResult> MoveElementImpl(int projectId, int plotElementId, int parentCharacterId, int direction)
    {
      var field = await ProjectRepository.GetCharacterAsync(projectId, parentCharacterId);
      var error = AsMaster(field, acl => acl.CanEditRoles);
      if (error != null)
      {
        return error;
      }

      try
      {
        await _plotService.MoveElement(CurrentUserId, projectId, plotElementId, parentCharacterId, direction);


        return RedirectToAction("Details", "Character", new {projectId, characterId = parentCharacterId});
      }
      catch
      {
        return RedirectToAction("Details", "Character", new { projectId, characterId = parentCharacterId });
      }
    }

    public Task<ActionResult> MoveDownElementForCharacter(int projectid, int plotelementid, int characterid)
    {
      return MoveElementImpl(projectid, plotelementid, characterid, +1);
    }
  }
}