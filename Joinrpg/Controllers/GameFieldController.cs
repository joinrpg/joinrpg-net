using System;
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
      IProjectService projectService) : base(userManager, projectRepository, projectService)
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
      try
      {
        await ProjectService.AddCharacterField(project.ProjectId, CurrentUserId, viewModel.FieldType, viewModel.Name,
          viewModel.FieldHint,
          viewModel.CanPlayerEdit, viewModel.CanPlayerView, viewModel.IsPublic);

        return ReturnToIndex(project);
      }
      catch
      {
        return View();
      }
    }

    [HttpGet]
    // GET: GameFields/Edit/5
    public ActionResult Edit(int projectId, int projectCharacterFieldId)
    {
      return WithGameFieldAsMaster(projectId, projectCharacterFieldId,
        (project, field) => View(new GameFieldEditViewModel(field)));
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
    public ActionResult Delete(int projectId, int projectCharacterFieldId)
    {
      return WithGameFieldAsMaster(projectId, projectCharacterFieldId, (project, field) => View(field));

    }

    // POST: GameFields/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(int projectId, int projectCharacterFieldId, FormCollection collection)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      var field = project.AllProjectFields.SingleOrDefault(e => e.ProjectCharacterFieldId == projectCharacterFieldId);

      var error = AsMaster(field, pa => pa.CanChangeFields);
      if (error != null)
      {
        return error;
      }
      {
        try
        {
          await ProjectService.DeleteField(field.ProjectCharacterFieldId);

          return ReturnToIndex(project);
        }
        catch
        {
          return View(field);
        }
      }


    }

    private ActionResult WithGameFieldAsMaster(int projectId, int fieldId,
      Func<Project, ProjectCharacterField, ActionResult> action)
    {
      var project1 = ProjectRepository.GetProject(projectId);
      var field = project1.AllProjectFields.SingleOrDefault(e => e.ProjectCharacterFieldId == fieldId);
      return AsMaster(field,pa => pa.CanChangeFields) ?? action(project1, field);
    }
  }
}