using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class GameFieldController : ControllerGameBase
  {
    private IFieldSetupService FieldSetupService { get; }

    public GameFieldController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService, IFieldSetupService fieldSetupService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      FieldSetupService = fieldSetupService;
    }

    private ActionResult ReturnToIndex(Project project)
    {
      return RedirectToAction("Index", new { project.ProjectId });
    }


    [HttpGet]
    // GET: GameFields
    public async Task<ActionResult> Index(int projectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project) ?? View(new GameFieldListViewModel()
      {
        ProjectId = project.ProjectId,
        Items = project.GetOrderedFields().ToViewModels(),
        CanEditFields = project.HasMasterAccess(CurrentUserId, pa => pa.CanChangeFields)
      });
    }

    [HttpGet]
    // GET: GameFields/Create
    public async Task<ActionResult> Create(int projectId)
    {
      var project1 = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project1, pa => pa.CanChangeFields) ?? View(new GameFieldCreateViewModel() {ProjectId = projectId});
    }

    // POST: GameFields/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(GameFieldCreateViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var error = AsMaster(project, pa => pa.CanChangeFields);
      if (error != null)
      {
        return error;
      }
      if (!ModelState.IsValid)
      {
        return View(viewModel);
      }
      try
      {
        await FieldSetupService.AddField(project.ProjectId, CurrentUserId, (ProjectFieldType) viewModel.FieldViewType, viewModel.Name,
          viewModel.Description.Contents,
          viewModel.CanPlayerEdit, viewModel.CanPlayerView,
          viewModel.IsPublic, (FieldBoundTo) viewModel.FieldBoundTo);

        return ReturnToIndex(project);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return View(viewModel);
      }
    }

    [HttpGet]
    // GET: GameFields/Edit/5
    public async Task<ActionResult> Edit(int projectId, int projectFieldId)
    {
      var field = await ProjectRepository.GetProjectField(projectId, projectFieldId);
      return AsMaster(field, pa => pa.CanChangeFields) ?? View(new GameFieldEditViewModel(field));
    }

    // POST: GameFields/Edit/5
    [HttpPost, Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(GameFieldEditViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var field = project.ProjectFields.SingleOrDefault(e => e.ProjectFieldId == viewModel.ProjectFieldId);

      var error = AsMaster(field, pa => pa.CanChangeFields);
      if (error != null || field == null)
      {
        return error;
      }
      if (!ModelState.IsValid)
      {
        return View(viewModel);
      }
      try
      {
        await
          FieldSetupService.UpdateFieldParams(CurrentUserId, project.ProjectId, field.ProjectFieldId,
            viewModel.Name, viewModel.Description.Contents, viewModel.CanPlayerEdit, viewModel.CanPlayerView,
            viewModel.IsPublic);

        return ReturnToIndex(project);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return View(viewModel);
      }
    }

    [HttpGet]
    // GET: GameFields/Delete/5
    public async Task<ActionResult> Delete(int projectId, int projectFieldId)
    {
      var field = await ProjectRepository.GetProjectField(projectId, projectFieldId);
      return AsMaster(field, pa => pa.CanChangeFields) ?? View(field);
    }

    // POST: GameFields/Delete/5
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
      {
        try
        {
          await FieldSetupService.DeleteField(field.ProjectFieldId);

          return ReturnToIndex(field.Project);
        }
        catch
        {
          return View(field);
        }
      }
    }

    [HttpGet]
    public async Task<ActionResult> CreateValue(int projectId, int projectFieldId)
    {
      var field = await ProjectRepository.GetProjectField(projectId, projectFieldId);
      return AsMaster(field, pa => pa.CanChangeFields) ?? View(new GameFieldDropdownValueCreateViewModel(field));
    }

    [HttpPost,ValidateAntiForgeryToken]
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
          FieldSetupService.CreateFieldValueVariant(field.ProjectId, field.ProjectFieldId, CurrentUserId, viewModel.Label,
            viewModel.Description.Contents);

        return RedirectToAction("Edit", new {viewModel.ProjectId, projectFieldId = viewModel.ProjectFieldId});
      }
      catch
      {
        return View(viewModel);
      }
    }

    [HttpGet]
    public async Task<ActionResult> EditValue(int projectFieldDropdownValueId, int projectId, int projectFieldId)
    {
      var value = await ProjectRepository.GetFieldValue(projectId, projectFieldId, projectFieldDropdownValueId);
      return AsMaster(value, pa => pa.CanChangeFields) ?? View(new GameFieldDropdownValueEditViewModel(value));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> EditValue(GameFieldDropdownValueEditViewModel viewModel)
    {
      var value =
        await
          ProjectRepository.GetFieldValue(viewModel.ProjectId, viewModel.ProjectFieldId, viewModel.ProjectFieldDropdownValueId);

      var error = AsMaster(value, pa => pa.CanChangeFields);
      if (error != null)
      {
        return error;
      }
      try
      {
        await
          FieldSetupService.UpdateFieldValueVariant(value.ProjectId, value.ProjectFieldDropdownValueId, CurrentUserId,
            viewModel.Label, viewModel.Description.Contents, viewModel.ProjectFieldId);

        return RedirectToAction("Edit", new {viewModel.ProjectId, projectFieldId = viewModel.ProjectFieldId});
      }
      catch
      {
        return View(viewModel);
      }
    }

    [HttpGet]
    public async Task<ActionResult> DeleteValue(int projectFieldDropdownValueId, int projectId, int projectFieldId)
    {
      var value = await ProjectRepository.GetFieldValue(projectId, projectFieldId, projectFieldDropdownValueId);
      return AsMaster(value, pa => pa.CanChangeFields) ?? View(new GameFieldDropdownValueEditViewModel(value));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteValue(GameFieldDropdownValueEditViewModel viewModel)
    {
      var value =
        await
          ProjectRepository.GetFieldValue(viewModel.ProjectId, viewModel.ProjectFieldId, viewModel.ProjectFieldDropdownValueId);

      var error = AsMaster(value, pa => pa.CanChangeFields);
      if (error != null)
      {
        return error;
      }
      try
      {
        await
          FieldSetupService.DeleteFieldValueVariant(value.ProjectId, value.ProjectFieldDropdownValueId, CurrentUserId, viewModel.ProjectFieldId);

        return RedirectToAction("Edit", new { viewModel.ProjectId, projectFieldId = viewModel.ProjectFieldId });
      }
      catch
      {
        return View(viewModel);
      }
    }

    public Task<ActionResult> MoveUp(int projectfieldid, int projectid)
    {
      return MoveImpl(projectfieldid, projectid, -1);
    }

    public Task<ActionResult> MoveDown(int projectfieldid, int projectid)
    {
      return MoveImpl(projectfieldid, projectid, +1);
    }


    private async Task<ActionResult> MoveImpl(int projectfieldid, int projectid, short direction)
    {
      var value =
        await
          ProjectRepository.GetProjectField(projectid, projectfieldid);
      var error = AsMaster(value, acl => acl.CanEditRoles);
      if (error != null)
      {
        return error;
      }

      try
      {
        await FieldSetupService.MoveField(CurrentUserId, projectid, projectfieldid, direction);


        return ReturnToIndex(value.Project);
      }
      catch
      {
        return ReturnToIndex(value.Project);
      }
    }
  }
}