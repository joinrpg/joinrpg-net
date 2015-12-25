using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class GameFieldController : ControllerGameBase
  {
    public GameFieldController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
    }

    private ActionResult ReturnToIndex(Project project)
    {
      return RedirectToAction("Index", new { project.ProjectId });
    }


    [HttpGet]
    // GET: GameFields
    public async Task<ActionResult> Index(int projectId)
    {
      var project1 = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project1, pa => pa.CanChangeFields) ?? View(new GameFieldListViewModel()
      {
        ProjectId = project1.ProjectId,
        Items = project1.AllProjectFields.Select(pf => new GameFieldEditViewModel(pf))
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
        await ProjectService.AddCharacterField(project.ProjectId, CurrentUserId, viewModel.FieldType, viewModel.Name,
          viewModel.FieldHint,
          viewModel.CanPlayerEdit, viewModel.CanPlayerView, viewModel.IsPublic);

        return ReturnToIndex(project);
      }
      catch
      {
        return View(viewModel);
      }
    }

    [HttpGet]
    // GET: GameFields/Edit/5
    public async Task<ActionResult> Edit(int projectId, int projectCharacterFieldId)
    {
      var field = await ProjectRepository.GetProjectField(projectId, projectCharacterFieldId);
      return AsMaster(field, pa => pa.CanChangeFields) ?? View(new GameFieldEditViewModel(field));
    }

    // POST: GameFields/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(GameFieldEditViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var field = project.AllProjectFields.SingleOrDefault(e => e.ProjectCharacterFieldId == viewModel.ProjectCharacterFieldId);

      var error = AsMaster(field, pa => pa.CanChangeFields);
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
        await ProjectService.UpdateCharacterField(project.ProjectId, field.ProjectCharacterFieldId, viewModel.Name,
          viewModel.FieldHint,
          viewModel.CanPlayerEdit, viewModel.CanPlayerView, viewModel.IsPublic);

        return ReturnToIndex(project);
      }
      catch
      {
        return View(viewModel);
      }
    }

    [HttpGet]
    // GET: GameFields/Delete/5
    public async Task<ActionResult> Delete(int projectId, int projectCharacterFieldId)
    {
      var field = await ProjectRepository.GetProjectField(projectId, projectCharacterFieldId);
      return AsMaster(field, pa => pa.CanChangeFields) ?? View(field);
    }

    // POST: GameFields/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(int projectId, int projectCharacterFieldId, FormCollection collection)
    {
      var field = await ProjectRepository.GetProjectField(projectId, projectCharacterFieldId);

      var error = AsMaster(field, pa => pa.CanChangeFields);
      if (error != null)
      {
        return error;
      }
      {
        try
        {
          await ProjectService.DeleteField(field.ProjectCharacterFieldId);

          return ReturnToIndex(field.Project);
        }
        catch
        {
          return View(field);
        }
      }
    }

    [HttpGet]
    public async Task<ActionResult> CreateValue(int projectId, int projectCharacterFieldId)
    {
      var field = await ProjectRepository.GetProjectField(projectId, projectCharacterFieldId);
      return AsMaster(field, pa => pa.CanChangeFields) ?? View(new GameFieldDropdownValueCreateViewModel(field));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateValue(GameFieldDropdownValueCreateViewModel viewModel)
    {
      var field = await ProjectRepository.GetProjectField(viewModel.ProjectId, viewModel.ProjectCharacterFieldId);

      var error = AsMaster(field, pa => pa.CanChangeFields);
      if (error != null)
      {
        return error;
      }
      try
      {
        await
          ProjectService.CreateFieldValue(field.ProjectId, field.ProjectCharacterFieldId, CurrentUserId, viewModel.Label,
            viewModel.Description);

        return ReturnToIndex(field.Project);
      }
      catch
      {
        return View(viewModel);
      }
    }

    [HttpGet]
    public async Task<ActionResult> EditValue(int projectCharacterFieldDropdownValueId, int projectId, int projectCharacterFieldId)
    {
      var value = await ProjectRepository.GetFieldValue(projectId, projectCharacterFieldDropdownValueId);
      return AsMaster(value, pa => pa.CanChangeFields) ?? View(new GameFieldDropdownValueEditViewModel(value));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> EditValue(GameFieldDropdownValueEditViewModel viewModel)
    {
      var value =
        await
          ProjectRepository.GetFieldValue(viewModel.ProjectId, viewModel.ProjectCharacterFieldDropdownValueId);

      var error = AsMaster(value, pa => pa.CanChangeFields);
      if (error != null)
      {
        return error;
      }
      try
      {
        await
          ProjectService.UpdateFieldValue(value.ProjectId, value.ProjectCharacterFieldDropdownValueId, CurrentUserId,
            viewModel.Label, viewModel.Description);

        return RedirectToAction("Edit", new {viewModel.ProjectId, viewModel.ProjectCharacterFieldId});
      }
      catch
      {
        return View(viewModel);
      }
    }

    [HttpGet]
    public async Task<ActionResult> DeleteValue(int projectCharacterFieldDropdownValueId, int projectId, int projectCharacterFieldId)
    {
      var value = await ProjectRepository.GetFieldValue(projectId, projectCharacterFieldDropdownValueId);
      return AsMaster(value, pa => pa.CanChangeFields) ?? View(new GameFieldDropdownValueEditViewModel(value));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteValue(GameFieldDropdownValueEditViewModel viewModel)
    {
      var value =
        await
          ProjectRepository.GetFieldValue(viewModel.ProjectId, viewModel.ProjectCharacterFieldDropdownValueId);

      var error = AsMaster(value, pa => pa.CanChangeFields);
      if (error != null)
      {
        return error;
      }
      try
      {
        await
          ProjectService.DeleteFieldValue(value.ProjectId, value.ProjectCharacterFieldDropdownValueId, CurrentUserId);

        return RedirectToAction("Edit", new { viewModel.ProjectId, viewModel.ProjectCharacterFieldId });
      }
      catch
      {
        return View(viewModel);
      }
    }
  }
}