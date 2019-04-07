using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.FieldSetup;
using JoinRpg.WebPortal.Managers;
using JoinRpg.WebPortal.Managers.Interfaces;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class GameFieldController : ControllerGameBase
  {
    private IFieldSetupService FieldSetupService { get; }
        public FieldSetupManager Manager { get; }
        private ICurrentProjectAccessor CurrentProjectAccessor { get; }

        public GameFieldController(
            ApplicationUserManager userManager,
            IProjectRepository projectRepository,
            IProjectService projectService,
            IExportDataService exportDataService,
            IFieldSetupService fieldSetupService,
            IUserRepository userRepository,
            FieldSetupManager manager,
            ICurrentProjectAccessor currentProjectAccessor
            )
          : base(userManager, projectRepository, projectService, exportDataService, userRepository)
        {
            FieldSetupService = fieldSetupService;
            Manager = manager;
            CurrentProjectAccessor = currentProjectAccessor;
        }

        private ActionResult ReturnToIndex()
            => RedirectToAction("Index", CurrentProjectAccessor.ProjectId);

        private ActionResult ReturnToField(ProjectField value)
            => RedirectToAction("Edit", new { CurrentProjectAccessor.ProjectId, projectFieldId = value.ProjectFieldId });


        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> Index(int projectId)
        {
            var model = await Manager.GetActiveAsync();
            return ViewIfFound(model);
        }

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> DeletedList(int projectId)
        {
            var model = await Manager.GetInActiveAsync();
            return ViewIfFound("Index", model);
        }

        [HttpGet, MasterAuthorize(Permission.CanChangeFields)]
        public async Task<ActionResult> Create(int projectId)
        {
            var model = await Manager.CreatePageAsync();
            return ViewIfFound(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [MasterAuthorize(Permission.CanChangeFields)]
        public async Task<ActionResult> Create(GameFieldCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return ViewIfFound(Manager.FillFailedModel(viewModel));
            }
            try
            {
                var request = new CreateFieldRequest(
                    viewModel.ProjectId,
                    (ProjectFieldType) viewModel.FieldViewType,
                    viewModel.Name,
                    viewModel.DescriptionEditable,
                    viewModel.CanPlayerEdit,
                    viewModel.CanPlayerView,
                    viewModel.IsPublic,
                    (FieldBoundTo) viewModel.FieldBoundTo,
                    (MandatoryStatus) viewModel.MandatoryStatus,
                    viewModel.ShowForGroups.GetUnprefixedGroups(),
                    viewModel.ValidForNpc,
                    viewModel.FieldBoundTo == FieldBoundToViewModel.Character && viewModel.CanPlayerView,
                    viewModel.ShowForUnApprovedClaim,
                    viewModel.Price,
                    viewModel.MasterDescriptionEditable,
                    programmaticValue: null);

                await FieldSetupService.AddField(request);

                return ReturnToIndex();
            }
            catch (Exception exception)
            {
                ModelState.AddException(exception);
                return View(Manager.FillFailedModel(viewModel));
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
        return HttpNotFound();
      }
      if (!ModelState.IsValid)
      {
        viewModel.FillNotEditable(field, CurrentUserId);
        return View(viewModel);
      }
      try
      {
          var request = new UpdateFieldRequest(project.ProjectId,
              viewModel.Name,
              viewModel.DescriptionEditable,
              viewModel.CanPlayerEdit,
              viewModel.CanPlayerView,
              viewModel.IsPublic,
              (MandatoryStatus) viewModel.MandatoryStatus,
              viewModel.ShowForGroups.GetUnprefixedGroups(),
              viewModel.ValidForNpc,
              viewModel.IncludeInPrint,
              viewModel.ShowForUnApprovedClaim,
              viewModel.Price,
              viewModel.MasterDescriptionEditable,
              field.ProjectFieldId,
              viewModel.ProgrammaticValue);

          await FieldSetupService.UpdateFieldParams(request);

        return ReturnToIndex();
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        viewModel.FillNotEditable(field, CurrentUserId);
        return View(viewModel);
      }
    }

        /// <summary>
        /// Removes custom field by HTTP DELETE request
        /// </summary>
        /// <param name="projectId">Id of a project to delete field from</param>
        /// <param name="projectFieldId">If of a field to delete</param>
        /// <returns>
        /// 200 -- if a field was successfully deleted
        /// 250 -- if a field was marked as inactive
        /// 500 -- if any exception occured
        /// 401 -- if logged user is not authorized to delete fields
        /// 404 -- if no field or project found
        /// </returns>
        [HttpDelete]        
        public async Task<ActionResult> DeleteEx(int projectId, int projectFieldId)
        {
            try
            {
                ProjectField field = await ProjectRepository.GetProjectField(projectId, projectFieldId);

                if (field == null)
                    return HttpNotFound();

                if (AsMaster(field, pa => pa.CanChangeFields) != null)
                    return new HttpUnauthorizedResult();

                await FieldSetupService.DeleteField(field);
                return field.IsActive
                    ? new HttpStatusCodeResult(200)
                    : new HttpStatusCodeResult(250);
            }
            catch(Exception)
            {
                // TODO: Implement exception logging here
                return new HttpStatusCodeResult(500);
            }
        }

        [HttpGet]
        public async Task<ActionResult> Delete(int projectId, int projectFieldId)
        {
            HttpStatusCodeResult ar = await DeleteEx(projectId, projectFieldId) as HttpStatusCodeResult;
            if (ar != null && ar.StatusCode >= 300)
                return ar;
            return ReturnToIndex();
        }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // ReSharper disable once UnusedParameter.Global
    public async Task<ActionResult> Delete(int projectId, int projectFieldId, FormCollection collection)
    {
      var field = await ProjectRepository.GetProjectField(projectId, projectFieldId);

      var error = AsMaster(field, pa => pa.CanChangeFields);
      if (error != null)
      {
        return error;
      }
      var project = field.Project;
      try
      {
        await FieldSetupService.DeleteField(projectId, field.ProjectFieldId);

        return ReturnToIndex();
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return View(field);
      }
    }

    [HttpGet]
    public async Task<ActionResult> CreateValue(int projectId, int projectFieldId)
    {
      var field = await ProjectRepository.GetProjectField(projectId, projectFieldId);
      return AsMaster(field, pa => pa.CanChangeFields) ?? View(new GameFieldDropdownValueCreateViewModel(field));
    }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateValue(GameFieldDropdownValueCreateViewModel viewModel)
        {
            var field = await ProjectRepository.GetProjectField(viewModel.ProjectId, viewModel.ProjectFieldId);

            var error = AsMaster(field, pa => pa.CanChangeFields);
            if (error != null)
            {
                return error;
            }
            try
            {
                await
                    FieldSetupService.CreateFieldValueVariant(
                        new CreateFieldValueVariantRequest(
                            field.ProjectId,
                            viewModel.Label,
                            viewModel.Description,
                            viewModel.ProjectFieldId,
                            viewModel.MasterDescription,
                            viewModel.ProgrammaticValue,
                            viewModel.Price,
                            viewModel.PlayerSelectable));

                return RedirectToAction("Edit", new { viewModel.ProjectId, projectFieldId = viewModel.ProjectFieldId });
            }
            catch
            {
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditValue(int projectId, int projectFieldId, int valueId)
        {
            var field = await ProjectRepository.GetProjectField(projectId, projectFieldId);
            var value = await ProjectRepository.GetFieldValue(projectId, projectFieldId, valueId);
            return AsMaster(value, pa => pa.CanChangeFields) ?? View(new GameFieldDropdownValueEditViewModel(field, value));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> EditValue(GameFieldDropdownValueEditViewModel viewModel)
        {
            var value = await ProjectRepository.GetFieldValue(viewModel.ProjectId, viewModel.ProjectFieldId, viewModel.ProjectFieldDropdownValueId);

            var error = AsMaster(value, pa => pa.CanChangeFields);
            if (error != null)
            {
                return error;
            }
            try
            {
                await FieldSetupService.UpdateFieldValueVariant(new UpdateFieldValueVariantRequest(
                    value.ProjectId,
                    value.ProjectFieldDropdownValueId,
                    viewModel.Label,
                    viewModel.Description,
                    viewModel.ProjectFieldId,
                    viewModel.MasterDescription,
                    viewModel.ProgrammaticValue,
                    viewModel.Price,
                    viewModel.PlayerSelectable));

                return RedirectToAction("Edit", new { viewModel.ProjectId, projectFieldId = viewModel.ProjectFieldId });
            }
            catch
            {
                return View(viewModel);
            }
        }


        /// <summary>
        /// Removes custom field value by HTTP DELETE request
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
        [HttpDelete]
        public async Task<ActionResult> DeleteValueEx(int projectId, int projectFieldId, int valueId)
        {
            try
            {
                var value = await ProjectRepository.GetFieldValue(projectId, projectFieldId, valueId);

                if (value == null)
                    return HttpNotFound();
                                
                if (AsMaster(value, pa => pa.CanChangeFields) != null)
                    return new HttpUnauthorizedResult();

                await FieldSetupService.DeleteFieldValueVariant(value.ProjectId, value.ProjectFieldId, value.ProjectFieldDropdownValueId);
                return value.IsActive
                    ? new HttpStatusCodeResult(200)
                    : new HttpStatusCodeResult(250);
            }
            catch (Exception)
            {
                // TODO: Implement exception logging here
                return new HttpStatusCodeResult(500);
            }
        }

        [MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> Move(int projectid, int listItemId, short direction)
    {
      var value = await ProjectRepository.GetProjectField(projectid, listItemId);
      
      if (value == null)
      {
        return HttpNotFound();
      }

      try
      {
        await FieldSetupService.MoveField(projectid, listItemId, direction);

        return ReturnToIndex();
      }
      catch
      {
        return ReturnToIndex();
      }
    }

    [MasterAuthorize(Permission.CanChangeFields)]
    public async Task<ActionResult> MoveValue(int projectid, int listItemId, int parentObjectId, short direction)
    {
      var value = await ProjectRepository.GetProjectField(projectid, parentObjectId);

      if (value == null)
      {
        return HttpNotFound();
      }

      try
      {
        await FieldSetupService.MoveFieldVariant(projectid, parentObjectId, listItemId, direction);


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
      var value = await ProjectRepository.GetProjectField(projectId, projectFieldId);

      if (value == null)
      {
        return HttpNotFound();
      }

      try
      {
        await FieldSetupService.CreateFieldValueVariants(projectId, projectFieldId, valuesToAdd);


        return ReturnToField(value);
      }
      catch
      {
        return ReturnToField(value);
      }
    }

    public async Task<ActionResult> MoveFast(int projectId, int projectFieldId, int? afterFieldId)
    {
      var value = await ProjectRepository.GetProjectField(projectId, projectFieldId);

      if (value == null)
      {
        return HttpNotFound();
      }

      if (afterFieldId == -1)
      {
        afterFieldId = null;
      }

      try
      {
        await FieldSetupService.MoveFieldAfter(projectId, projectFieldId, afterFieldId);


        return ReturnToIndex();
      }
      catch
      {
        return ReturnToIndex();
      }
    }
  }
}
