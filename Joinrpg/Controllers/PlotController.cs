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

    [MasterAuthorize(AllowPublish = true)]
    public async Task<ActionResult> Index(int projectId)
    {
      return await PlotList(projectId, pf => true);
    }

    [MasterAuthorize(AllowPublish = true)]
    public async Task<ActionResult> InWork(int projectId)
    {
      return await PlotList(projectId, pf => pf.InWork);
    }

    [MasterAuthorize(AllowPublish = true)]
    public async Task<ActionResult> Ready(int projectId)
    {
      return await PlotList(projectId, pf => pf.Completed);
    }

    [MasterAuthorize(AllowPublish = true)]
    public async Task<ActionResult> ByTag(int projectid, string tagname)
    {
      var allFolders = await _plotRepository.GetPlotsByTag(projectid, tagname);
      var project = await GetProjectFromList(projectid, allFolders);
      return View("Index", new PlotFolderListViewModel(allFolders, project, CurrentUserIdOrDefault));
    }

    [MasterAuthorize(AllowPublish = true)]
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

      return View("ForGroup", new PlotFolderListViewModelForGroup(folders, project, CurrentUserIdOrDefault, groupNavigation));
    }

    [MasterAuthorize(AllowPublish = true)]
    public async Task<ActionResult> FlatList(int projectId)
    {
      var folders = (await _plotRepository.GetPlotsWithTargetAndText(projectId)).ToList(); 
      var project = await GetProjectFromList(projectId, folders);
      return View(
               new PlotFolderFullListViewModel(
                 folders, 
                 project, 
                 CurrentUserIdOrDefault));
    }

    [MasterAuthorize(AllowPublish = true)]
    public async Task<ActionResult> FlatListUnready(int projectId)
    {
      var folders = (await _plotRepository.GetPlotsWithTargetAndText(projectId)).ToList();
      var project = await GetProjectFromList(projectId, folders);
      return View("FlatList",
               new PlotFolderFullListViewModel(folders, project, CurrentUserIdOrDefault, true));
    }

    public PlotController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IPlotService plotService, IPlotRepository plotRepository,
      IExportDataService exportDataService) : base(userManager, projectRepository, projectService, exportDataService)
    {
      _plotService = plotService;
      _plotRepository = plotRepository;
    }

    [HttpGet, MasterAuthorize(Permission.CanManagePlots)]
    public async Task<ActionResult> Create(int projectId)
    {
      var project1 = await ProjectRepository.GetProjectAsync(projectId);
      return View(new AddPlotFolderViewModel
      {
        ProjectId = project1.ProjectId,
        ProjectName = project1.ProjectName
      });
    }

      [HttpPost, ValidateAntiForgeryToken, MasterAuthorize(Permission.CanManagePlots)]
      public async Task<ActionResult> Create(AddPlotFolderViewModel viewModel)
      {
          if (!ModelState.IsValid)
          {
              return View(viewModel);
          }

          try
          {
              await _plotService.CreatePlotFolder(viewModel.ProjectId,
                  viewModel.PlotFolderTitleAndTags, viewModel.TodoField);
              return RedirectToAction("Index", "Plot", new {viewModel.ProjectId});
          }
          catch (Exception exception)
          {
              ModelState.AddException(exception);
              return View(viewModel);
          }
      }

      [HttpGet, MasterAuthorize(AllowPublish = true)]
    public async Task<ActionResult> Edit(int projectId, int plotFolderId)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      if (folder == null)
      {
        return HttpNotFound();
      }
      return View(new EditPlotFolderViewModel(folder, CurrentUserIdOrDefault));
    }

    [HttpPost, ValidateAntiForgeryToken, MasterAuthorize(Permission.CanManagePlots)]
    public async Task<ActionResult> Edit(EditPlotFolderViewModel viewModel)
    {
      try
      {
        await
          _plotService.EditPlotFolder(viewModel.ProjectId, viewModel.PlotFolderId, viewModel.PlotFolderTitleAndTags, viewModel.TodoField);
        return ReturnToPlot(viewModel.ProjectId, viewModel.PlotFolderId);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        var folder = await _plotRepository.GetPlotFolderAsync(viewModel.ProjectId, viewModel.PlotFolderId);
        viewModel.Fill(folder, CurrentUserId);
        return View(viewModel);
      }
    }

    #region Create elements & handouts
    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> CreateElement(int projectId, int plotFolderId)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      if (folder == null)
      {
        return HttpNotFound();
      }
      return View(new AddPlotElementViewModel()
      {
        ProjectId = projectId,
        PlotFolderId = plotFolderId,
        PlotFolderName = folder.MasterTitle,
      });
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> CreateHandout(int projectId, int plotFolderId)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      if (folder == null)
      {
        return HttpNotFound();
      }
      return View(new AddPlotHandoutViewModel()
      {
        ProjectId = projectId,
        PlotFolderId = plotFolderId,
        PlotFolderName = folder.MasterTitle,
      });
    }

    [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateHandout(int projectId, int plotFolderId, string content,
      string todoField, [CanBeNull] ICollection<string> targets, PlotElementTypeView elementType)
    {
      try
      {
        return await CreateElementImpl(projectId, plotFolderId, content, todoField, targets, elementType);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
        if (folder == null)
        {
          return HttpNotFound();
        }
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

    [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateElement(int projectId, int plotFolderId, string content,
      string todoField, [CanBeNull] ICollection<string> targets, PlotElementTypeView elementType)
    {
      try
      {
        return await CreateElementImpl(projectId, plotFolderId, content, todoField, targets, elementType);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
        if (folder == null)
        {
          return HttpNotFound();
        }
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
      return View("Index", new PlotFolderListViewModel(folders, project, CurrentUserIdOrDefault));
    }

    #endregion

    [HttpGet, MasterAuthorize(Permission.CanManagePlots)]
    public async Task<ActionResult> Delete(int projectId, int plotFolderId)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      if (folder == null)
      {
        return HttpNotFound();
      }
      return View(new EditPlotFolderViewModel(folder, CurrentUserId));
    }

    [HttpPost, MasterAuthorize(Permission.CanManagePlots), ValidateAntiForgeryToken]
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

    [HttpPost, MasterAuthorize(Permission.CanManagePlots), ValidateAntiForgeryToken]
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

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> EditElement(int plotelementid, int plotFolderId, int projectId)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      if (folder == null)
      {
        return HttpNotFound();
      }
      var viewModel = new EditPlotElementViewModel(folder.Elements.Single(e => e.PlotElementId == plotelementid),
        folder.HasMasterAccess(CurrentUserId, acl => acl.CanManagePlots));
      return View(viewModel);
    }

    [HttpPost, MasterAuthorize()]
    public async Task<ActionResult> EditElement(int plotelementid, int plotFolderId, int projectId, string content, string todoField,
      [CanBeNull] ICollection<string> targets)
    {
      try
      {
        var project = await ProjectRepository.GetProjectAsync(projectId);
        if (project.HasMasterAccess(CurrentUserId, acl => acl.CanManagePlots))
        {
          var targetGroups = targets.OrEmptyList().GetUnprefixedGroups();
          var targetChars = targets.OrEmptyList().GetUnprefixedChars();
          await
            _plotService.EditPlotElement(projectId, plotFolderId, plotelementid, content, todoField, targetGroups,
              targetChars);
        }
        else
        {
          await
            _plotService.EditPlotElementText(projectId, plotFolderId, plotelementid, content, todoField);
        }
        return ReturnToPlot(projectId, plotFolderId);
      }
      catch (Exception)
      {
        return await Edit(projectId, plotFolderId);
      }
    }

    [MasterAuthorize(Permission.CanEditRoles), ValidateAntiForgeryToken, HttpPost]

    public async Task<ActionResult> MoveElementForCharacter(int projectid, int listItemId, int parentObjectId, int direction)
    {
      try
      {
        await _plotService.MoveElement(projectid, listItemId, parentObjectId, direction);


        return RedirectToAction("Details", "Character", new {projectId = projectid, characterId = parentObjectId });
      }
      catch
      {
        return RedirectToAction("Details", "Character", new {projectId = projectid, characterId = parentObjectId });
      }
    }


    [HttpPost, MasterAuthorize(Permission.CanManagePlots), ValidateAntiForgeryToken]
    public async Task<ActionResult> PublishElement(int plotelementid, int plotFolderId, int projectId, int version)
    {
      try
      {
        await _plotService.PublishElementVersion(projectId, plotFolderId, plotelementid, version);
        return ReturnToPlot(projectId, plotFolderId);
      }
      catch (Exception)
      {
        return await Edit(projectId, plotFolderId);
      }
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ShowElementVersion(int projectId, int plotFolderId, int plotElementId, int version)
    {
      var folder = await _plotRepository.GetPlotFolderAsync(projectId, plotFolderId);
      if (folder == null)
      {
        return HttpNotFound();
      }
      return View(new PlotElementListItemViewModel(folder.Elements.Single(e => e.PlotElementId == plotElementId),
        CurrentUserId, version));
    }

    [HttpPost, MasterAuthorize(Permission.CanManagePlots), ValidateAntiForgeryToken]
    public async Task<ActionResult> UnPublishElement(int plotelementid, int plotFolderId, int projectId)
    {
      try
      {
        await _plotService.PublishElementVersion(projectId, plotFolderId, plotelementid, null);
        return ReturnToPlot(projectId, plotFolderId);
      }
      catch (Exception)
      {
        return await Edit(projectId, plotFolderId);
      }
    }
  }
}