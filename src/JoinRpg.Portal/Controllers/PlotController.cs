using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.Helpers;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.Plots;
using JoinRpg.WebComponents.ElementMoving;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/plot/[action]")]
public class PlotController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    IPlotService plotService,
    IPlotRepository plotRepository,
    IUriService uriService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    ILogger<PlotController> logger
    ) : ControllerGameBase(projectRepository,
        projectService)
{
    [MasterAuthorize(Permission.CanManagePlots)]
    [HttpGet]
    public async Task<ActionResult> Create(int projectId)
    {
        var project1 = await ProjectRepository.GetProjectAsync(projectId);
        return View(new AddPlotFolderViewModel
        {
            ProjectId = project1.ProjectId,
            ProjectName = project1.ProjectName,
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
            await plotService.CreatePlotFolder(new(viewModel.ProjectId),
                viewModel.PlotFolderTitleAndTags, viewModel.TodoField);
            return RedirectToAction("Index", "PlotList", new { viewModel.ProjectId });
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            return View(viewModel);
        }
    }

    [HttpGet, RequireMasterOrPublish]
    public async Task<ActionResult> Edit(int projectId, int plotFolderId)
    {
        var folder = await plotRepository.GetPlotFolderAsync(new PlotFolderIdentification(projectId, plotFolderId));
        if (folder == null)
        {
            return NotFound();
        }
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        return View(new EditPlotFolderViewModel(folder, currentUserAccessor, uriService, projectInfo));
    }

    [HttpPost, ValidateAntiForgeryToken, MasterAuthorize(Permission.CanManagePlots)]
    public async Task<ActionResult> Edit(EditPlotFolderViewModel viewModel)
    {
        try
        {
            await
              plotService.EditPlotFolder(viewModel.ProjectId, viewModel.PlotFolderId, viewModel.PlotFolderTitleAndTags, viewModel.TodoField);
            return ReturnToPlot(viewModel.ProjectId, viewModel.PlotFolderId);
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            var folder = await plotRepository.GetPlotFolderAsync(new(viewModel.ProjectId, viewModel.PlotFolderId));
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(viewModel.ProjectId));
            viewModel.Fill(folder, currentUserAccessor, uriService, projectInfo);
            return View(viewModel);
        }
    }

    #region Create elements & handouts
    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> CreateElement(ProjectIdentification projectId, int? plotFolderId, PlotElementIdentification? copyFrom)
    {
        var folders = await plotRepository.GetPlots(projectId);
        if (folders.Count == 0)
        {
            return RedirectToAction("Create", "Plot", new { projectId = projectId.Value });
        }

        PlotElement? originalElement = null;
        if (copyFrom is not null)
        {
            var originalElementFolder = await plotRepository.GetPlotFolderAsync(copyFrom.PlotFolderId);
            if (originalElementFolder is not null && originalElementFolder.ProjectId == projectId)
            {
                originalElement = originalElementFolder.Elements.Single(e => e.PlotElementId == copyFrom.PlotElementId);
            }
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        return View(new AddPlotElementViewModel()
        {
            ProjectId = projectId,
            PlotFolderId = plotFolderId ?? copyFrom?.PlotFolderId?.PlotFolderId,
            HasPlotEditAccess = projectInfo.HasMasterAccess(currentUserAccessor, Permission.CanManagePlots),
            Content = originalElement?.LastVersion().Content.Contents ?? AddPlotElementViewModel.GetDefaultContent(),
            TodoField = originalElement?.LastVersion().TodoField ?? "",
            Targets = originalElement?.GetElementBindingsForEdit() ?? [],
        });
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> CreateHandout(int projectId, int plotFolderId)
    {
        var folder = await plotRepository.GetPlotFolderAsync(new(projectId, plotFolderId));
        if (folder == null)
        {
            return NotFound();
        }
        return View(new AddPlotHandoutViewModel()
        {
            ProjectId = projectId,
            PlotFolderId = plotFolderId,
            PlotFolderName = folder.MasterTitle,
        });
    }

    [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateHandout(ProjectIdentification projectId, int plotFolderId, string content,
      string todoField, ICollection<string>? targets, PlotElementTypeView elementType)
    {
        PlotFolderIdentification plotFolderId1 = new(projectId, plotFolderId);
        try
        {

            return await CreateElementImpl(plotFolderId1, content, todoField, targets ?? [], elementType, publishNow: false);
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            var folder = await plotRepository.GetPlotFolderAsync(plotFolderId1);
            if (folder == null)
            {
                return NotFound();
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
    public async Task<ActionResult> CreateElement(ProjectIdentification projectId, int plotFolderId, string content,
      string todoField, ICollection<string>? targets, PlotElementTypeView elementType, bool publishNow)
    {
        PlotFolderIdentification plotFolderId1 = new(projectId, plotFolderId);
        try
        {
            return await CreateElementImpl(plotFolderId1, content, todoField, targets ?? [], elementType, publishNow);
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            var folder = await plotRepository.GetPlotFolderAsync(plotFolderId1);
            if (folder == null)
            {
                return NotFound();
            }
            return View(new AddPlotElementViewModel()
            {
                ProjectId = projectId,
                PlotFolderId = plotFolderId,
                Content = content,
                TodoField = todoField,
                HasPlotEditAccess = folder.HasMasterAccess(currentUserAccessor, Permission.CanManagePlots),
            });
        }
    }

    private async Task<ActionResult> CreateElementImpl(PlotFolderIdentification plotFolderId, string content, string todoField, ICollection<string> targets,
      PlotElementTypeView elementType, bool publishNow)
    {
        var targetGroups = targets.OrEmptyList().GetUnprefixedGroups(plotFolderId.ProjectId);
        var targetChars = targets.OrEmptyList().GetUnprefixedChars(plotFolderId.ProjectId);
        var versionId = await
          plotService.CreatePlotElement(plotFolderId, content, todoField, targetGroups, targetChars,
            (PlotElementType)elementType);

        if (publishNow && string.IsNullOrWhiteSpace(todoField) && targets.Count != 0)
        {
            await plotService.PublishElementVersion(versionId, sendNotification: false, commentText: null);
        }
        return ReturnToPlot(plotFolderId);
    }
    #endregion

    [HttpGet, MasterAuthorize(Permission.CanManagePlots)]
    public async Task<ActionResult> Delete(int projectId, int plotFolderId)
    {
        var folder = await plotRepository.GetPlotFolderAsync(new(projectId, plotFolderId));
        if (folder == null)
        {
            return NotFound();
        }
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        return View(new EditPlotFolderViewModel(folder, currentUserAccessor, uriService, projectInfo));
    }

    [HttpPost, MasterAuthorize(Permission.CanManagePlots), ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(int projectId, int plotFolderId, IFormCollection collection)
    {
        try
        {
            await plotService.DeleteFolder(projectId, plotFolderId);
            return RedirectToAction("Index", "PlotList", new { projectId });
        }
        catch (Exception)
        {
            return await Delete(projectId, plotFolderId);
        }
    }
    private RedirectToActionResult ReturnToPlot(int projectId, int plotFolderId) => RedirectToAction("Edit", new { projectId, plotFolderId });

    private RedirectToActionResult ReturnToPlot(PlotFolderIdentification plotFolderId) => RedirectToAction("Edit", new { projectId = plotFolderId.ProjectId.Value, plotFolderId = plotFolderId.PlotFolderId });

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> EditElement(ProjectIdentification projectId, PlotElementIdentification elementId, int? version)
    {
        if (elementId.ProjectId != projectId)
        {
            return NotFound();
        }
        var folder = await plotRepository.GetPlotFolderAsync(elementId.PlotFolderId);
        if (folder == null)
        {
            return NotFound();
        }
        var viewModel = new EditPlotElementViewModel(
            folder.Elements.Single(e => e.PlotElementId == elementId.PlotElementId),
            folder.HasMasterAccess(CurrentUserId, acl => acl.CanManagePlots),
            version
            );
        return View(viewModel);
    }

    [HttpPost, MasterAuthorize()]
    public async Task<ActionResult> EditElement(int plotelementid, int plotFolderId, ProjectIdentification projectId, string content, string todoField,
      ICollection<string>? targets)
    {
        var id = new PlotElementIdentification(projectId, plotFolderId, plotelementid);
        try
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);
            if (project.HasMasterAccess(CurrentUserId, acl => acl.CanManagePlots))
            {
                var targetGroups = targets.OrEmptyList().GetUnprefixedGroups(projectId);
                var targetChars = targets.OrEmptyList().GetUnprefixedChars(projectId);

                await
                  plotService.EditPlotElement(id, content, todoField, targetGroups, targetChars);
            }
            else
            {
                await
                  plotService.EditPlotElementText(id, content, todoField);
            }
            return ReturnToPlot(id.PlotFolderId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка при изменении вводной {plotElementId}", plotelementid);
            ModelState.AddException(ex);
            return await Edit(projectId, plotFolderId);
        }
    }

    //TODO: Make this POST
    [HttpGet]
    [MasterAuthorize(Permission.CanEditRoles)]

    public async Task<ActionResult> MoveElementForCharacter(int projectid, int listItemId, int parentObjectId, int direction)
    {
        try
        {
            await plotService.MoveElement(projectid, listItemId, parentObjectId, direction);


            return RedirectToAction("Details", "Character", new { projectId = projectid, characterId = parentObjectId });
        }
        catch
        {
            return RedirectToAction("Details", "Character", new { projectId = projectid, characterId = parentObjectId });
        }
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> ShowElementVersion(int projectId, int plotFolderId, int plotElementId, int version, bool printMode)
    {
        var folder = await plotRepository.GetPlotFolderAsync(new(projectId, plotFolderId));
        if (folder == null)
        {
            return NotFound();
        }
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        return View(new PlotElementListItemViewModel(folder.Elements.Single(e => e.PlotElementId == plotElementId),
          CurrentUserId,
            itemIdsToParticipateInSort: null, renderer: new JoinrpgMarkdownLinkRenderer(folder.Project, projectInfo), currentVersion: version, printMode: printMode));
    }

    [HttpPost(), MasterAuthorize(Permission.CanManagePlots)]
    public async Task<RedirectToActionResult> ReorderFolder(ProjectIdentification projectId, ElementMoveCommandViewModel viewModel)
    {
        var plotFolderId = PlotFolderIdentification.Parse(viewModel.ElementIdentification, provider: null);
        var afterPlotFolderId = PlotFolderIdentification.TryParse(viewModel.MoveAfterIdentification, provider: null, out var r) ? r : null;
        await plotService.ReorderPlots(plotFolderId, afterPlotFolderId);
        return RedirectToAction("Index", "PlotList", new { projectId = projectId.Value });
    }

    [HttpPost(), MasterAuthorize(Permission.CanManagePlots)]
    public async Task<RedirectToActionResult> ReorderElements(ProjectIdentification projectId, ElementMoveCommandViewModel viewModel)
    {
        var targetId = PlotElementIdentification.Parse(viewModel.ElementIdentification, provider: null);
        var afterId = PlotElementIdentification.TryParse(viewModel.MoveAfterIdentification, provider: null, out var r) ? r : null;
        await plotService.ReorderPlotElements(targetId, afterId);
        return RedirectToAction("Edit", "Plot", new { projectId = projectId.Value, PlotFolderId = targetId.PlotFolderId.PlotFolderId });
    }

    [HttpPost(), MasterAuthorize(Permission.CanManagePlots)]
    public async Task<RedirectToActionResult> ReorderByCharacter(ProjectIdentification projectId, int characterId, ElementMoveCommandViewModel viewModel)
    {
        var targetId = PlotElementIdentification.Parse(viewModel.ElementIdentification, provider: null);
        var afterId = PlotElementIdentification.TryParse(viewModel.MoveAfterIdentification, provider: null, out var r) ? r : null;
        await plotService.ReorderPlotByChar(new CharacterIdentification(projectId, characterId), targetId, afterId);
        return RedirectToAction("Details", "Character", new { projectId = projectId.Value, characterId });
    }

}
