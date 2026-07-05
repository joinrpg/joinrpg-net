using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.ProjectCommon.Fields;
using JoinRpg.WebPortal.Managers;
using JoinRpg.WebPortal.Managers.Interfaces;
using JoinRpg.WebPortal.Models.FieldSetup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Authorize]
[Route("{ProjectId}/fields/[action]")]
public class GameFieldController(
    IProjectRepository projectRepository,
    IFieldSetupService fieldSetupService,
    FieldSetupManager manager,
    ICurrentProjectAccessor currentProjectAccessor,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    ILogger<GameFieldController> logger
        ) : JoinControllerGameBase
{
    public FieldSetupManager Manager { get; } = manager;

    private RedirectToActionResult ReturnToIndex()
        => RedirectToAction("Index", new { ProjectId = currentProjectAccessor.ProjectId.Value });

    private RedirectToActionResult ReturnToField(ProjectFieldIdentification projectFieldId)
        => RedirectToAction("Edit", new { ProjectId = projectFieldId.ProjectId.Value, projectFieldId.ProjectFieldId });


    [HttpGet("/{ProjectId}/fields/")]
    [MasterAuthorize]
    public async Task<ActionResult> Index(ProjectIdentification projectId)
    {
        var model = await Manager.GetActiveAsync();
        return ViewIfFound(model);
    }

    [HttpGet("/{ProjectId}/fields/archive")]
    [MasterAuthorize]
    public async Task<ActionResult> DeletedList(ProjectIdentification projectId)
    {
        var model = await Manager.GetInActiveAsync();
        return ViewIfFound("Index", model);
    }

    [HttpGet]
    [MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> Create(ProjectIdentification projectId)
    {
        var model = await Manager.CreatePageAsync();
        return ViewIfFound(model);
    }

    [HttpGet]
    [MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> Settings()
    {
        var model = await Manager.SettingsPagesAsync();
        return ViewIfFound(model);
    }

    [HttpPost]
    [MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> Settings(FieldSettingsViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return await ViewIfFound(Manager.FillFailedSettingsModel(viewModel));
        }
        try
        {
            await Manager.SettingsHandleAsync(viewModel);

            return ReturnToIndex();
        }
        catch (Exception exception)
        {
            AddModelException(exception);
            return View(await Manager.FillFailedSettingsModel(viewModel));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> Create(GameFieldCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return await ViewIfFound(Manager.FillFailedModel(viewModel));
        }
        try
        {
            var request = new CreateFieldRequest(
                new(viewModel.ProjectId),
                (ProjectFieldType)viewModel.FieldViewType,
                viewModel.Name,
                viewModel.DescriptionEditable,
                viewModel.CanPlayerEdit,
                viewModel.CanPlayerView,
                viewModel.IsPublic,
                (FieldBoundTo)viewModel.FieldBoundTo,
                (MandatoryStatus)viewModel.MandatoryStatus,
                CharacterGroupIdentification.FromList(viewModel.ShowForGroupsInts, new ProjectIdentification(viewModel.ProjectId)).ToList(),
                viewModel.ValidForNpc,
                viewModel.FieldBoundTo == FieldBoundToViewModel.Character && viewModel.CanPlayerView,
                viewModel.ShowForUnApprovedClaim,
                viewModel.Price,
                viewModel.MasterDescriptionEditable,
                programmaticValue: null);

            await fieldSetupService.AddField(request);

            return ReturnToIndex();
        }
        catch (Exception exception)
        {
            AddModelException(exception);
            return View(await Manager.FillFailedModel(viewModel));
        }
    }

    [HttpGet, MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> Edit(ProjectIdentification projectId, int projectFieldId)
    {
        var model = await Manager.EditPageAsync(new(projectId, projectFieldId));
        return ViewIfFound(model);
    }

    [HttpPost, MasterAuthorize(Permission.CanChangeFields)]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(GameFieldEditViewModel viewModel)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(viewModel.ProjectId));
        var field = projectInfo.UnsortedFields.SingleOrDefault(f => f.Id.ProjectFieldId == viewModel.ProjectFieldId);

        if (field is null)
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            viewModel.FillNotEditable(field, projectInfo);
            return View(viewModel);
        }
        try
        {
            var request = new UpdateFieldRequest(new ProjectFieldIdentification(new ProjectIdentification(projectInfo.ProjectId), field.Id.ProjectFieldId),
                viewModel.Name,
                viewModel.DescriptionEditable,
                viewModel.CanPlayerEdit,
                viewModel.CanPlayerView,
                viewModel.IsPublic,
                (MandatoryStatus)viewModel.MandatoryStatus,
                 CharacterGroupIdentification.FromList(viewModel.ShowForGroupsInts, new ProjectIdentification(viewModel.ProjectId)).ToList(),
                viewModel.ValidForNpc,
                viewModel.IncludeInPrint,
                viewModel.ShowForUnApprovedClaim,
                viewModel.Price,
                viewModel.MasterDescriptionEditable,
                viewModel.ProgrammaticValue);

            await fieldSetupService.UpdateFieldParams(request);

            return ReturnToIndex();
        }
        catch (Exception exception)
        {
            AddModelException(exception);
            viewModel.FillNotEditable(field, projectInfo);
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> Delete(int projectId, int projectFieldId, IFormCollection collection)
    {
        var field = await projectRepository.GetProjectField(projectId, projectFieldId);

        try
        {
            await fieldSetupService.DeleteField(projectId, field.ProjectFieldId);

            return ReturnToIndex();
        }
        catch (Exception exception)
        {
            AddModelException(exception);
            return View(field);
        }
    }

    [HttpGet]
    [MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> CreateValue(int projectId, int projectFieldId)
    {
        var id = new ProjectFieldIdentification(new(projectId), projectFieldId);
        var metadata = await projectMetadataRepository.GetProjectMetadata(id.ProjectId);
        var field = metadata.GetFieldById(id);
        return View(new GameFieldDropdownValueCreateViewModel(field));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> CreateValue(GameFieldDropdownValueCreateViewModel viewModel)
    {
        try
        {
            var id = new ProjectFieldIdentification(new(viewModel.ProjectId), viewModel.ProjectFieldId);
            var metadata = await projectMetadataRepository.GetProjectMetadata(id.ProjectId);
            var field = metadata.GetFieldById(id);

            var timeSlotOptions = viewModel.GetTimeSlotRequest(field.IsTimeSlot, Request.Form["TimeSlotStartTime"].FirstOrDefault());

            await
                fieldSetupService.CreateFieldValueVariant(
                    new CreateFieldValueVariantRequest(
                        id,
                        viewModel.Label,
                        viewModel.Description,
                        viewModel.MasterDescription,
                        viewModel.ProgrammaticValue,
                        viewModel.Price,
                        viewModel.PlayerSelectable,
                        timeSlotOptions));

            return RedirectToAction("Edit", new { viewModel.ProjectId, projectFieldId = viewModel.ProjectFieldId });
        }
        catch (Exception ex)
        {
            AddModelException(ex);
            return View(viewModel);
        }
    }

    [HttpGet]
    [MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> EditValue(int projectId, int projectFieldId, int valueId)
    {
        var id = new ProjectFieldIdentification(new(projectId), projectFieldId);
        var metadata = await projectMetadataRepository.GetProjectMetadata(id.ProjectId);

        var field = metadata.GetFieldById(id);
        var variant = field.Variants.SingleOrDefault(v => v.Id.ProjectFieldVariantId == valueId);
        if (variant == null)
        {
            return NotFound();
        }
        return View(new GameFieldDropdownValueEditViewModel(field, variant));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> EditValue(GameFieldDropdownValueEditViewModel viewModel)
    {
        try
        {
            var id = new ProjectFieldIdentification(new(viewModel.ProjectId), viewModel.ProjectFieldId);
            var metadata = await projectMetadataRepository.GetProjectMetadata(id.ProjectId);
            var field = metadata.GetFieldById(id);

            await fieldSetupService.UpdateFieldValueVariant(new UpdateFieldValueVariantRequest(
                id,
                viewModel.ProjectFieldDropdownValueId,
                viewModel.Label,
                viewModel.Description,
                viewModel.MasterDescription,
                viewModel.ProgrammaticValue,
                viewModel.Price,
                viewModel.PlayerSelectable,
                viewModel.GetTimeSlotRequest(field.IsTimeSlot, Request.Form["TimeSlotStartTime"].FirstOrDefault())
                ));

            return RedirectToAction("Edit", new { viewModel.ProjectId, projectFieldId = viewModel.ProjectFieldId });
        }
        catch (Exception ex)
        {
            AddModelException(ex);
            return View(viewModel);
        }
    }


    /// <summary>
    /// Removes custom field value by HTTP GET request
    /// </summary>
    /// <param name="projectId">Id of a project where field is located in</param>
    /// <param name="projectFieldId">Id of a field to delete value from</param>
    /// <param name="valueId">Id of a value to delete</param>
    /// <returns>
    /// 200 -- if a value was successfully deleted
    /// 250 -- if a value was marked as inactive
    /// 500 -- if any exception occured
    /// 401 -- if logged user is not authorized to delete values
    /// 404 -- if no field or project found
    /// </returns>
    [MasterAuthorize(Permission.CanChangeFields)]
    [HttpDelete("~/{projectId:int}/fields/{projectFieldId:int}/DeleteValueEx/{valueId:int}")]
    public async Task<ActionResult> DeleteValueEx(int projectId, int projectFieldId, int valueId)
    {
        try
        {
            var metadata = await projectMetadataRepository.GetProjectMetadata(new(projectId));
            var field = metadata.UnsortedFields.SingleOrDefault(f => f.Id.ProjectFieldId == projectFieldId);
            if (field is null)
            {
                return NotFound();
            }
            var variant = field.Variants.SingleOrDefault(v => v.Id.ProjectFieldVariantId == valueId);
            if (variant is null)
            {
                return NotFound();
            }

            _ = await fieldSetupService.DeleteFieldValueVariant(projectId, projectFieldId, valueId);
            return variant.IsActive
                ? Ok()
                : StatusCode(250);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка");
            // TODO: Implement exception logging here
            return StatusCode(500);
        }
    }

    [MasterAuthorize(Permission.CanChangeFields)]
    // TODO: Refactor to HEAD request (require UI fixes)
    [HttpGet("~/{projectId:int}/fields/{listItemId:int}/move/{direction:int}")]
    public async Task<ActionResult> Move(int projectId, int listItemId, int direction)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        if (metadata.UnsortedFields.All(f => f.Id.ProjectFieldId != listItemId))
        {
            return NotFound();
        }

        try
        {
            await fieldSetupService.MoveField(projectId, listItemId, (short)direction);

            return ReturnToIndex();
        }
        catch
        {
            return ReturnToIndex();
        }
    }

    [MasterAuthorize(Permission.CanChangeFields)]
    // TODO: Refactor to HEAD request (require UI fixes)
    [HttpGet("~/{projectId:int}/fields/{parentObjectId:int}/values/{listItemId:int}/move/{direction:int}")]
    public async Task<ActionResult> MoveValue(ProjectIdentification projectId, int listItemId, int parentObjectId, int direction)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (metadata.UnsortedFields.All(f => f.Id.ProjectFieldId != parentObjectId))
        {
            return NotFound();
        }

        try
        {
            await fieldSetupService.MoveFieldVariant(projectId, parentObjectId, listItemId, (short)direction);

            return ReturnToField(new ProjectFieldIdentification(projectId, parentObjectId));
        }
        catch
        {
            return ReturnToField(new ProjectFieldIdentification(projectId, parentObjectId));
        }
    }

    [HttpPost, MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> MassCreateValueVariants(int projectId, int projectFieldId, string valuesToAdd)
    {
        var id = new ProjectFieldIdentification(new(projectId), projectFieldId);
        var metadata = await projectMetadataRepository.GetProjectMetadata(id.ProjectId);
        if (metadata.UnsortedFields.All(f => f.Id.ProjectFieldId != id.ProjectFieldId))
        {
            return NotFound();
        }

        try
        {
            await fieldSetupService.CreateFieldValueVariants(id, valuesToAdd);

            return ReturnToField(id);
        }
        catch
        {
            return ReturnToField(id);
        }
    }



    [MasterAuthorize(Permission.CanChangeFields)]
    [HttpPost("~/{projectId:int}/fields/{projectFieldId:int}/sortvariants")]
    public async Task<ActionResult> SortVariants(int projectId, int projectFieldId)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        if (metadata.UnsortedFields.All(f => f.Id.ProjectFieldId != projectFieldId))
        {
            return NotFound();
        }

        try
        {
            await fieldSetupService.SortFieldVariants(projectId, projectFieldId);

            return ReturnToIndex();
        }
        catch
        {
            return ReturnToIndex();
        }
    }
}
