using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Allrpg;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Controllers
{
  public class GameController : Common.ControllerGameBase
  {
    private readonly IAllrpgService _allrpgService;

    public GameController(IProjectService projectService, ApplicationUserManager userManager,
      IProjectRepository projectRepository, IAllrpgService allrpgService) : base(userManager, projectRepository, projectService)
    {
      _allrpgService = allrpgService;
    }

    // GET: Game/Details/5
    public async Task<ActionResult> Details(int projectId)
    {
      var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
      return WithEntity(project) ?? View(new ProjectDetailsViewModel()
      {
        ProjectAnnounce = new MarkdownViewModel(project.Details?.ProjectAnnounce),
        ProjectId = project.ProjectId,
        ProjectName = project.ProjectName,
        IsActive = project.Active,
        CreatedDate = project.CreatedDate,
        Masters = project.ProjectAcls.Select(acl => acl.User)
      });
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
    public async Task<ActionResult> Create(ProjectCreateViewModel model)
    {
      try
      {
        var project = await ProjectService.AddProject(model.ProjectName, GetCurrentUser());

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
    public async Task<ActionResult> Edit(int projectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project, pacl => pacl.CanChangeProjectProperties) ?? View(new EditProjectViewModel
      {
        ClaimApplyRules =new MarkdownViewModel(project.Details?.ClaimApplyRules),
        ProjectAnnounce = new MarkdownViewModel(project.Details?.ProjectAnnounce),
        ProjectId = project.ProjectId,
        ProjectName = project.ProjectName,
        OriginalName = project.ProjectName
      });
    }

    // POST: Game/Edit/5
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(EditProjectViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var errorResult = AsMaster(project, pacl => pacl.CanChangeProjectProperties);
      if (errorResult != null)
      {
        return errorResult;
      }

      try
      {
        await
          ProjectService.EditProject(viewModel.ProjectId, viewModel.ProjectName, viewModel.ClaimApplyRules.Contents,
            viewModel.ProjectAnnounce.Contents);

        return RedirectTo(project);
      }
      catch
      {
        viewModel.OriginalName = project.ProjectName;
        return View(viewModel);
      }
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> AllrpgUpdate(int projectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      var errorResult = AsMaster(project, pacl => pacl.IsOwner);
      if (errorResult != null)
      {
        return errorResult;
      }
      return View(new AllrpgUpdateViewModel() {ProjectId = projectId, ProjectName = project.ProjectName});
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> AllrpgUpdate(AllrpgUpdateViewModel model)
    {
      var project = await ProjectRepository.GetProjectAsync(model.ProjectId);
      var errorResult = AsMaster(project, pacl => pacl.IsOwner);
      if (errorResult != null)
      {
        return errorResult;
      }
      model.ProjectName = project.ProjectName;
      try
      {
        model.UpdateResult = string.Join("\n", await _allrpgService.UpdateProject(CurrentUserId, model.ProjectId));
        return View(model);
      }
      catch
      { 
        return View(model);
      }
      
    }
  }
}