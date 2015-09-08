using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class GameController : ControllerGameBase
  {
    public GameController(IProjectService projectService, ApplicationUserManager userManager,
      IProjectRepository projectRepository) : base(userManager, projectRepository, projectService)
    {
    }

    // GET: Game
    public ActionResult Index()
    {
      return View();
    }

    // GET: Game/Details/5
    public ActionResult Details(int projectId)
    {
      return WithProject(projectId, p => View(p));
    }

    // GET: Game/Create
    [Authorize]
    public ActionResult Create()
    {
      return View();
    }

    // POST: Game/Create
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(ProjectCreateViewModel model)
    {
      try
      {
        var project = ProjectService.AddProject(model.ProjectName, GetCurrentUser());

        return RedirectTo(project);
      }
      catch
      {
        return View(model);
      }
    }

    private ActionResult RedirectTo(Project project)
    {
      return RedirectToAction("Details", new {project.ProjectId});
    }

    [HttpGet, Authorize]
    // GET: Game/Edit/5
    public ActionResult Edit(int projectId)
    {
      return WithProjectAsMaster(projectId, pacl => pacl.CanChangeProjectProperties,
        project => View(new EditProjectViewModel
        {
          ClaimApplyRules = project.Details?.ClaimApplyRules?.Contents,
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName
        }));
    }

    // POST: Game/Edit/5
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public ActionResult Edit(EditProjectViewModel viewModel)
    {
      return WithProjectAsMaster(viewModel.ProjectId, pacl => pacl.CanChangeProjectProperties, 
        project =>
      {
        try
        {
          ProjectService.EditProject(viewModel.ProjectId, viewModel.ProjectName, viewModel.ClaimApplyRules);

          return RedirectTo(project);
        }
        catch
        {
          return View(viewModel);
        }
      });
    }

  }
}