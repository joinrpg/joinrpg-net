using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Controllers
{
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


    public async Task<ActionResult> ByTag(int projectid, string tagname)
    {
      var allFolders = await _plotRepository.GetPlotsByTag(projectid, tagname);
      var project = await GetProjectFromList(projectid, allFolders);
      return WithPlot(project) ?? View("Index", new PlotFolderListViewModel(allFolders, project, CurrentUserIdOrDefault));
    }

    public async Task<ActionResult> ForGroup(int projectId, int characterGroupId)
    {
      var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
      if (group == null)
      {
        return HttpNotFound();
      }

      //TODO slow 
      var characterGroups = group.GetChildrenGroups().Union(new[] {group}).ToList();
      var characters = characterGroups.SelectMany(g => g.Characters).Distinct().Select(c => c.CharacterId).ToList();
      var characterGroupIds = characterGroups.Select(c => c.CharacterGroupId).ToList();
      var folders = await _plotRepository.GetPlotsForTargets(projectId, characters, characterGroupIds);
      var project = group.Project;

      var groupNavigation = new CharacterGroupDetailsViewModel(group, CurrentUserIdOrDefault, GroupNavigationPage.Plots);

      return  WithPlot(project) ?? View("ForGroup", new PlotFolderListViewModelForGroup(folders, project, CurrentUserIdOrDefault, groupNavigation));
    }


    public async Task<ActionResult> FlatList(int projectId)
    {
      var folders = (await _plotRepository.GetPlotsWithTargetAndText(projectId)).ToList(); 
      var project = await GetProjectFromList(projectId, folders);
      return WithPlot(project) ??
             View(
               new PlotFolderFullListViewModel(
                 folders, 
                 project, 
                 CurrentUserIdOrDefault));
    }

    public async Task<ActionResult> FlatListUnready(int projectId)
    {
      var folders = (await _plotRepository.GetPlotsWithTargetAndText(projectId)).ToList();
      var project = await GetProjectFromList(projectId, folders);
      return WithPlot(project) ??
             View("FlatList",
               new PlotFolderFullListViewModel(folders, project, CurrentUserIdOrDefault, true));
    }

    public PlotController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IPlotService plotService, IPlotRepository plotRepository,
      IExportDataService exportDataService) : base(userManager, projectRepository, projectService, exportDataService)
    {
      _plotService = plotService;
      _plotRepository = plotRepository;
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Create(int projectId)
    {
      var project1 = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project1, acl => acl.CanManagePlots) ??  View(new AddPlotFolderViewModel
      {
        ProjectId = project1.ProjectId,
        ProjectName = project1.ProjectName
      });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(AddPlotFolderViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var errorResult = AsMaster(project, acl => acl.CanManagePlots);
      if (errorResult != null)
      {
        return errorResult;
      }

      try
      {
        await _plotService.CreatePlotFolder(project.ProjectId, viewModel.PlotFolderTitleAndTags, viewModel.TodoField);
        return RedirectToAction("Index", "Plot", new {project.ProjectId});
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return View(viewModel);
      }

    }

    [HttpGet]
    public async Task<ActionResult> Edit(int projectId, int plotFolderId)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      return WithPlot(folder) ?? View(new EditPlotFolderViewModel(folder, CurrentUserIdOrDefault));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
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
          _plotService.EditPlotFolder(viewModel.ProjectId, viewModel.PlotFolderId, viewModel.PlotFolderTitleAndTags, viewModel.TodoField);
        return ReturnToPlot(viewModel.ProjectId, viewModel.PlotFolderId);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        viewModel.Fill(folder, CurrentUserId);
        return View(viewModel);
      }
    }

    #region Create elements & handouts
    [HttpGet, Authorize]
    public async Task<ActionResult> CreateElement(int projectId, int plotFolderId)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      return AsMaster(folder, acl => acl.CanManagePlots) ?? View(new AddPlotElementViewModel()
      {
        ProjectId = projectId,
        PlotFolderId = plotFolderId,
        PlotFolderName = folder.MasterTitle,
      });
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> CreateHandout(int projectId, int plotFolderId)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      return AsMaster(folder, acl => acl.CanManagePlots) ?? View(new AddPlotHandoutViewModel()
      {
        ProjectId = projectId,
        PlotFolderId = plotFolderId,
        PlotFolderName = folder.MasterTitle,
      });
    }

    [HttpPost, Authorize]
    public async Task<ActionResult> CreateHandout(int projectId, int plotFolderId, string content,
      string todoField, [CanBeNull] ICollection<string> targets, PlotElementTypeView elementType)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      var error = AsMaster(folder, acl => acl.CanManagePlots);
      if (error != null)
      {
        return error;
      }
      try
      {
        return await CreateElementImpl(projectId, plotFolderId, content, todoField, targets, elementType);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return View(new AddPlotHandoutViewModel()
        {
          ProjectId = projectId,
          PlotFolderId = plotFolderId,
          PlotFolderName = folder.MasterTitle,
          Content = content,
          TodoField = todoField,
        });
      }
    }

    [HttpPost, Authorize]
    public async Task<ActionResult> CreateElement(int projectId, int plotFolderId, string content,
      string todoField, [CanBeNull] ICollection<string> targets, PlotElementTypeView elementType)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      var error = AsMaster(folder, acl => acl.CanManagePlots);
      if (error != null)
      {
        return error;
      }
      try
      {
        return await CreateElementImpl(projectId, plotFolderId, content, todoField, targets, elementType);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return View(new AddPlotElementViewModel()
        {
          ProjectId = projectId,
          PlotFolderId = plotFolderId,
          PlotFolderName = folder.MasterTitle,
          Content = content,
          TodoField = todoField,
        });
      }
    }

    private async Task<ActionResult> CreateElementImpl(int projectId, int plotFolderId, string content, string todoField, ICollection<string> targets,
      PlotElementTypeView elementType)
    {
      var targetGroups = targets.OrEmptyList().GetUnprefixedGroups();
      var targetChars = targets.OrEmptyList().GetUnprefixedChars();
      await
        _plotService.CreatePlotElement(projectId, plotFolderId, content, todoField, targetGroups, targetChars,
          (PlotElementType)elementType);
      return ReturnToPlot(projectId, plotFolderId);
    }
    #endregion

    #region private methods

    private async Task<ActionResult> PlotList(int projectId, Func<PlotFolder, bool> predicate)
    {
      var allFolders = await _plotRepository.GetPlots(projectId);
      var folders = allFolders.Where(predicate).ToList(); //Sadly, we have to do this, as we can't query using complex properties
      var project = await GetProjectFromList(projectId, folders);
      return WithPlot(project) ?? View("Index", new PlotFolderListViewModel(folders, project, CurrentUserIdOrDefault));
    }

    #endregion

    [HttpGet, Authorize]
    public async Task<ActionResult> Delete(int projectId, int plotFolderId)
    {
      PlotFolder folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      var error = AsMaster(folder, acl=> acl.CanManagePlots);
      if (error != null) return null;
      return View(new EditPlotFolderViewModel(folder, CurrentUserId));
    }

    [HttpPost, Authorize]
    public async Task<ActionResult> Delete(int projectId, int plotFolderId, [UsedImplicitly] FormCollection collection)
    {
      try
      {
        await _plotService.DeleteFolder(projectId, plotFolderId);
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
        await _plotService.DeleteElement(projectId, plotFolderId, plotelementid);
        return ReturnToPlot(projectId, plotFolderId);
      }
      catch (Exception)
      {
        return await Edit(projectId, plotFolderId);
      }
    }

    private ActionResult ReturnToPlot(int projectId, int plotFolderId)
    {
      return RedirectToAction("Edit", new {projectId, plotFolderId});
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> EditElement(int plotelementid, int plotFolderId, int projectId)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      var error = AsMaster(folder, acl => acl.CanManagePlots);
      if (error != null)
      {
        return error;
      }

      var viewModel = new EditPlotElementViewModel(folder.Elements.Single(e => e.PlotElementId == plotelementid));
      return View(viewModel);
    }

    [HttpPost, Authorize]
    public async Task<ActionResult> EditElement(int plotelementid, int plotFolderId, int projectId, string content, string todoField,
      bool isCompleted, [CanBeNull] ICollection<string> targets)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      var error = AsMaster(folder, acl => acl.CanManagePlots);
      if (error != null)
      {
        return error;
      }
      try
      {

        var targetGroups = targets.OrEmptyList().GetUnprefixedGroups();
        var targetChars = targets.OrEmptyList().GetUnprefixedChars();
        await
          _plotService.EditPlotElement(projectId, plotFolderId, plotelementid, content, todoField, targetGroups, targetChars, isCompleted);
        return ReturnToPlot(plotFolderId, projectId);
      }
      catch (Exception)
      {
        return await Edit(projectId, plotFolderId);
      }
    }


    public Task<ActionResult> MoveElementForCharacter(int projectid, int listItemId, int parentObjectId, int direction)
    {
      return MoveElementImpl(projectid, listItemId, parentObjectId, direction);
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
        await _plotService.MoveElement(projectId, plotElementId, parentCharacterId, direction);


        return RedirectToAction("Details", "Character", new { projectId, characterId = parentCharacterId });
      }
      catch
      {
        return RedirectToAction("Details", "Character", new { projectId, characterId = parentCharacterId });
      }
    }


    [HttpPost, MasterAuthorize(Permission.CanManagePlots)]
    public async Task<ActionResult> PublishElement(int plotelementid, int plotFolderId, int projectId)
    {
      try
      {
        await _plotService.PublishElement(projectId, plotFolderId, plotelementid);
        return ReturnToPlot(projectId, plotFolderId);
      }
      catch (Exception)
      {
        return await Edit(projectId, plotFolderId);
      }
    }
  }
}