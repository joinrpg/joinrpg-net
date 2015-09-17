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
      return RedirectToAction("Index", new {project.ProjectId});
    }

    [HttpGet]
    [Authorize]
    // GET: GameFields
    public async Task<ActionResult> Index(int projectId)
    {
      var project1 = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project1, pa => pa.CanChangeFields) ?? View(project1);
    }

    // GET: GameFields/Create
    public async Task<ActionResult> Create(int projectId)
    {
      var project1 = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project1, pa => pa.CanChangeFields) ?? View(new ProjectCharacterField() {ProjectId = projectId});
    }

    // POST: GameFields/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(ProjectCharacterField field)
    {
      var project1 = ProjectRepository.GetProject(field.ProjectId);
      return AsMaster(project1, pa => pa.CanChangeFields) ?? ((Func<Project, ActionResult>) (project =>
      {
        try
        {
          field.IsActive = true;
          ProjectService.AddCharacterField(field);

          return ReturnToIndex(project);
        }
        catch
        {
          return View();
        }
      }))(project1);
    }

    // GET: GameFields/Edit/5
    public ActionResult Edit(int projectId, int projectCharacterFieldId)
    {
      return WithGameFieldAsMaster(projectId, projectCharacterFieldId, (project, field) =>
      {
        var viewModel = new GameFieldEditViewModel()
        {
          CanPlayerView = field.CanPlayerView,
          CanPlayerEdit = field.CanPlayerEdit,
          FieldHint = field.FieldHint,
          FieldId = field.ProjectCharacterFieldId,
          IsPublic = field.IsPublic,
          Name = field.FieldName,
          ProjectId = field.ProjectId
        };
        return View(viewModel);
      });
    }

    // POST: GameFields/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(GameFieldEditViewModel viewModel)
    {
      return WithGameFieldAsMaster(viewModel.ProjectId, viewModel.FieldId, (project, field) =>
      {
        try
        {
          ProjectService.UpdateCharacterField(project.ProjectId, field.ProjectCharacterFieldId, viewModel.Name,
            viewModel.FieldHint,
            viewModel.CanPlayerEdit, viewModel.CanPlayerView, viewModel.IsPublic);

          return ReturnToIndex(project);
        }
        catch
        {
          return View(viewModel);
        }
      });
    }

    [HttpGet]
    [Authorize]
    // GET: GameFields/Delete/5
    public ActionResult Delete(int projectId, int projectCharacterFieldId)
    {
      return WithGameFieldAsMaster(projectId, projectCharacterFieldId, (project, field) => View(field));

    }

    // POST: GameFields/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Delete(int projectId, int projectCharacterFieldId, FormCollection collection)
    {
      return WithGameFieldAsMaster(projectId, projectCharacterFieldId, (project, field) =>
      {
        try
        {
          ProjectService.DeleteField(field.ProjectCharacterFieldId);

          return ReturnToIndex(project);
        }
        catch
        {
          return View(field);
        }
      });


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