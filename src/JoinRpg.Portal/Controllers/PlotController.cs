using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Helpers;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.Plots;
using JoinRpg.WebComponents.ElementMoving;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/plots/[action]")]
public class PlotController(
    IProjectRepository projectRepository,
    IPlotService plotService,
    IPlotRepository plotRepository,
    IUriService uriService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    ILogger<PlotController> logger
    ) : JoinControllerGameBase
{
    [MasterAuthorize(Permission.CanManagePlots)]
    [HttpGet]
    public async Task<ActionResult> Create(int projectId)
    {
        var project1 = await projectRepository.GetProjectAsync(projectId);
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
            AddModelException(exception);
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
            AddModelException(exception);
            var folder = await plotRepository.GetPlotFolderAsync(new(viewModel.ProjectId, viewModel.PlotFolderId));
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(viewModel.ProjectId));
            viewModel.Fill(folder, currentUserAccessor, uriService, projectInfo);
            return View(viewModel);
        }
    }

    #region Create elements
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
        var selectedPlotFolderId = plotFolderId ?? copyFrom?.PlotFolderId?.PlotFolderId;

        return View(new PlotElementCreateViewModel()
        {
            ProjectId = projectId,
            PlotFolderId = selectedPlotFolderId,
            ElementType = PlotElementTypeView.RegularPlot,
            HasPlotEditAccess = projectInfo.HasMasterAccess(currentUserAccessor, Permission.CanManagePlots),
            Content = originalElement?.LastVersion().Content.Contents ?? PlotElementCreateViewModel.GetDefaultContent(),
            TodoField = originalElement?.LastVersion().TodoField ?? "",
            TargetCharacters = [.. originalElement?.TargetCharacters?.Select(c => c.GetId()) ?? []],
            TargetGroups = [.. originalElement?.TargetGroups?.Select(g => g.GetId()) ?? []],
        });
    }

    [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateElement(ProjectIdentification projectId, int plotFolderId, string content,
      string todoField, ICollection<int>? targetCharacters, ICollection<int>? targetGroups, PlotElementTypeView elementType, bool publishNow)
    {
        PlotFolderIdentification plotFolderId1 = new(projectId, plotFolderId);
        var targetCharIds = CharacterIdentification.FromList(targetCharacters ?? [], projectId).ToList();
        var targetGroupIds = CharacterGroupIdentification.FromList(targetGroups ?? [], projectId).ToList();
        try
        {
            var versionId =
                await plotService.CreatePlotElement(plotFolderId1, content, todoField, targetGroupIds, targetCharIds, (PlotElementType)elementType);

            if (publishNow && string.IsNullOrWhiteSpace(todoField) && (targetGroupIds.Count != 0 || targetCharIds.Count != 0))
            {
                await plotService.PublishElementVersion(versionId, sendNotification: false, commentText: null);
            }
            return ReturnToPlot(plotFolderId1);
        }
        catch (Exception exception)
        {
            AddModelException(exception);
            var folder = await plotRepository.GetPlotFolderAsync(plotFolderId1);
            if (folder == null)
            {
                return NotFound();
            }
            return View(new PlotElementCreateViewModel()
            {
                ProjectId = projectId,
                PlotFolderId = plotFolderId,
                ElementType = elementType,
                Content = content,
                TodoField = todoField,
                TargetCharacters = [.. targetCharIds],
                TargetGroups = [.. targetGroupIds],
                HasPlotEditAccess = folder.HasMasterAccess(currentUserAccessor, Permission.CanManagePlots),
                PublishNow = publishNow,
            });
        }
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
        var element = folder.Elements.Single(e => e.PlotElementId == elementId.PlotElementId);
        var hasManageAccess = folder.HasMasterAccess(currentUserAccessor, Permission.CanManagePlots);
        var specificVersion = version is null ? element.LastVersion() : element.SpecificVersion(version.Value);

        var viewModel = new PlotElementEditViewModel()
        {
            ProjectId = element.PlotFolder.ProjectId,
            PlotFolderId = element.PlotFolderId,
            PlotElementId = element.PlotElementId,
            PlotFolderName = folder.MasterTitle,
            ElementType = (PlotElementTypeView)element.ElementType,
            Status = element.GetStatus(),
            HasManageAccess = hasManageAccess,
            HasPublishedVersion = element.Published != null,
            Target = element.ToTarget(),
            Content = specificVersion?.Content.Contents ?? "",
            TodoField = element.LastVersion().TodoField,
            TargetCharacters = [.. element.TargetCharacters.Select(c => new CharacterIdentification(new(element.ProjectId), c.CharacterId))],
            TargetGroups = [.. element.TargetGroups.Select(g => new CharacterGroupIdentification(new(element.ProjectId), g.CharacterGroupId))],
        };
        return View(viewModel);
    }

    [HttpPost, MasterAuthorize()]
    public async Task<ActionResult> EditElement(int plotelementid, int plotFolderId, ProjectIdentification projectId, string content, string todoField,
      ICollection<int>? targetCharacters, ICollection<int>? targetGroups)
    {
        var id = new PlotElementIdentification(projectId, plotFolderId, plotelementid);
        try
        {
            var project = await projectMetadataRepository.GetProjectMetadata(projectId);
            if (project.HasMasterAccess(currentUserAccessor, Permission.CanManagePlots))
            {
                var targetGroupIds = CharacterGroupIdentification.FromList(targetGroups ?? [], projectId).ToList();
                var targetCharIds = CharacterIdentification.FromList(targetCharacters ?? [], projectId).ToList();

                await plotService.EditPlotElement(id, content, todoField, targetGroupIds, targetCharIds);
            }
            else
            {
                await plotService.EditPlotElementText(id, content, todoField);
            }
            return ReturnToPlot(id.PlotFolderId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка при изменении вводной {plotElementId}", plotelementid);
            AddModelException(ex);
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
    public async Task<ActionResult> ShowElementVersion(ProjectIdentification projectId, int plotFolderId, int plotElementId, int version, bool printMode)
    {
        var folder = await plotRepository.GetPlotFolderAsync(new(projectId, plotFolderId));
        if (folder == null)
        {
            return NotFound();
        }
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        return View(new PlotElementListItemViewModel(folder.Elements.Single(e => e.PlotElementId == plotElementId),
          currentUserAccessor.UserId,
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
