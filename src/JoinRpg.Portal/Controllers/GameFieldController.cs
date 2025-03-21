using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.FieldSetup;
using JoinRpg.WebPortal.Managers;
using JoinRpg.WebPortal.Managers.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Authorize]
[Route("{ProjectId}/fields/[action]")]
public class GameFieldController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    IFieldSetupService fieldSetupService,
    IUserRepository userRepository,
    FieldSetupManager manager,
    ICurrentProjectAccessor currentProjectAccessor,
    IProjectMetadataRepository projectMetadataRepository
        ) : ControllerGameBase(projectRepository, projectService, userRepository)
{
    public FieldSetupManager Manager { get; } = manager;

    private ActionResult ReturnToIndex()
        => RedirectToAction("Index", new { ProjectId = currentProjectAccessor.ProjectId.Value });

    private ActionResult ReturnToField(ProjectField value)
        => RedirectToAction("Edit", new { ProjectId = currentProjectAccessor.ProjectId.Value, projectFieldId = value.ProjectFieldId });


    [HttpGet("/{ProjectId}/fields/")]
    [MasterAuthorize]
    public async Task<ActionResult> Index(int projectId)
    {
        var model = await Manager.GetActiveAsync();
        return ViewIfFound(model);
    }

    [HttpGet("/{ProjectId}/fields/archive")]
    [MasterAuthorize]
    public async Task<ActionResult> DeletedList(int projectId)
    {
        var model = await Manager.GetInActiveAsync();
        return ViewIfFound("Index", model);
    }

    [HttpGet]
    [MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> Create(int projectId)
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
            ModelState.AddException(exception);
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
                viewModel.ShowForGroups.GetUnprefixedGroups(new ProjectIdentification(viewModel.ProjectId)),
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
            ModelState.AddException(exception);
            return View(await Manager.FillFailedModel(viewModel));
        }
    }

    [HttpGet, MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> Edit(int projectId, int projectFieldId)
    {
        var model = await Manager.EditPageAsync(projectFieldId);
        return ViewIfFound(model);
    }

    [HttpPost, MasterAuthorize(Permission.CanChangeFields)]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(GameFieldEditViewModel viewModel)
    {
        var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
        var field = project.ProjectFields.SingleOrDefault(e => e.ProjectFieldId == viewModel.ProjectFieldId);

        if (field == null)
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            viewModel.FillNotEditable(field, CurrentUserId);
            return View(viewModel);
        }
        try
        {
            var request = new UpdateFieldRequest(new ProjectFieldIdentification(new ProjectIdentification(project.ProjectId), field.ProjectFieldId),
                viewModel.Name,
                viewModel.DescriptionEditable,
                viewModel.CanPlayerEdit,
                viewModel.CanPlayerView,
                viewModel.IsPublic,
                (MandatoryStatus)viewModel.MandatoryStatus,
                viewModel.ShowForGroups.GetUnprefixedGroups(new ProjectIdentification(project.ProjectId)),
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
            ModelState.AddException(exception);
            viewModel.FillNotEditable(field, CurrentUserId);
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MasterAuthorize(Permission.CanChangeFields)]
    // ReSharper disable once UnusedParameter.Global
    public async Task<ActionResult> Delete(int projectId, int projectFieldId, IFormCollection collection)
    {
        var field = await ProjectRepository.GetProjectField(projectId, projectFieldId);

        try
        {
            await fieldSetupService.DeleteField(projectId, field.ProjectFieldId);

            return ReturnToIndex();
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
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
            var field = await ProjectRepository.GetProjectField(viewModel.ProjectId, viewModel.ProjectFieldId);

            var timeSlotOptions = viewModel.GetTimeSlotRequest(field, Request.Form["TimeSlotStartTime"].FirstOrDefault());

            await
                fieldSetupService.CreateFieldValueVariant(
                    new CreateFieldValueVariantRequest(
                        new(new ProjectIdentification(viewModel.ProjectId), viewModel.ProjectFieldId),
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
            ModelState.AddException(ex);
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
            var field = await ProjectRepository.GetProjectField(viewModel.ProjectId, viewModel.ProjectFieldId);
            await fieldSetupService.UpdateFieldValueVariant(new UpdateFieldValueVariantRequest(
                new(new ProjectIdentification(viewModel.ProjectId), viewModel.ProjectFieldId),
                viewModel.ProjectFieldDropdownValueId,
                viewModel.Label,
                viewModel.Description,
                viewModel.MasterDescription,
                viewModel.ProgrammaticValue,
                viewModel.Price,
                viewModel.PlayerSelectable,
                viewModel.GetTimeSlotRequest(field, Request.Form["TimeSlotStartTime"].FirstOrDefault())
                ));

            return RedirectToAction("Edit", new { viewModel.ProjectId, projectFieldId = viewModel.ProjectFieldId });
        }
        catch
        {
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
            var value = await ProjectRepository.GetFieldValue(projectId, projectFieldId, valueId);

            if (value == null)
            {
                return NotFound();
            }

            _ = await fieldSetupService.DeleteFieldValueVariant(value.ProjectId, value.ProjectFieldId, value.ProjectFieldDropdownValueId);
            return value.IsActive
                ? Ok()
                : StatusCode(250);
        }
        catch (Exception)
        {
            // TODO: Implement exception logging here
            return StatusCode(500);
        }
    }

    [MasterAuthorize(Permission.CanChangeFields)]
    // TODO: Refactor to HEAD request (require UI fixes)
    [HttpGet("~/{projectId:int}/fields/{listItemId:int}/move/{direction:int}")]
    public async Task<ActionResult> Move(int projectId, int listItemId, int direction)
    {
        var value = await ProjectRepository.GetProjectField(projectId, listItemId);

        if (value == null)
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
    public async Task<ActionResult> MoveValue(int projectId, int listItemId, int parentObjectId, int direction)
    {
        var value = await ProjectRepository.GetProjectField(projectId, parentObjectId);

        if (value == null)
        {
            return NotFound();
        }

        try
        {
            await fieldSetupService.MoveFieldVariant(projectId, parentObjectId, listItemId, (short)direction);


            return ReturnToField(value);
        }
        catch
        {
            return ReturnToField(value);
        }
    }

    [HttpPost, MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> MassCreateValueVariants(int projectId, int projectFieldId, string valuesToAdd)
    {
        var id = new ProjectFieldIdentification(new(projectId), projectFieldId);
        var value = await ProjectRepository.GetProjectField(id);

        if (value == null)
        {
            return NotFound();
        }

        try
        {
            await fieldSetupService.CreateFieldValueVariants(id, valuesToAdd);


            return ReturnToField(value);
        }
        catch
        {
            return ReturnToField(value);
        }
    }

    [HttpPost, MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> MoveFast(int projectId, int projectFieldId, int? afterFieldId)
    {
        var value = await ProjectRepository.GetProjectField(projectId, projectFieldId);

        if (value == null)
        {
            return NotFound();
        }

        if (afterFieldId == -1)
        {
            afterFieldId = null;
        }

        try
        {
            await fieldSetupService.MoveFieldAfter(projectId, projectFieldId, afterFieldId);


            return ReturnToIndex();
        }
        catch
        {
            return ReturnToIndex();
        }
    }

    [MasterAuthorize(Permission.CanChangeFields)]
    [HttpPost("~/{projectId:int}/fields/{projectFieldId:int}/sortvariants")]
    public async Task<ActionResult> SortVariants(int projectId, int projectFieldId)
    {
        var value = await ProjectRepository.GetProjectField(projectId, projectFieldId);

        if (value == null)
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
