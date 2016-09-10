using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class GameController : Common.ControllerGameBase
  {
    public GameController(IProjectService projectService, ApplicationUserManager userManager,
      IProjectRepository projectRepository, IExportDataService exportDataService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
    }

    public async Task<ActionResult> Details(int projectId)
    {
      var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
      return WithEntity(project) ?? View(new ProjectDetailsViewModel(project));
    }

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
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return View(model);
      }
    }

    private ActionResult RedirectTo(Project project)
    {
      return RedirectToAction("Details", new {project.ProjectId});
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Edit(int projectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project, pacl => pacl.CanChangeProjectProperties) ?? View(new EditProjectViewModel
      {
        ClaimApplyRules =project.Details?.ClaimApplyRules.Contents,
        ProjectAnnounce = project.Details?.ProjectAnnounce.Contents,
        ProjectId = project.ProjectId,
        ProjectName = project.ProjectName,
        OriginalName = project.ProjectName,
        IsAcceptingClaims = project.IsAcceptingClaims,
        PublishPlot = project.Details?.PublishPlot ?? false,
        EnableManyCharacters = project.Details?.EnableManyCharacters ?? false,
        Active = project.Active
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
          ProjectService.EditProject(viewModel.ProjectId, CurrentUserId, viewModel.ProjectName, viewModel.ClaimApplyRules,
            viewModel.ProjectAnnounce, viewModel.IsAcceptingClaims, viewModel.EnableManyCharacters, viewModel.PublishPlot);

        return RedirectTo(project);
      }
      catch
      {
        viewModel.OriginalName = project.ProjectName;
        return View(viewModel);
      }
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Close(int projectid)
    {
      var project = await ProjectRepository.GetProjectAsync(projectid);
      var errorResult = await AsMasterOrAdmin(project, pacl => pacl.CanChangeProjectProperties);
      if (errorResult != null)
      {
        return errorResult;
      }
      var isMaster = project.HasMasterAccess(CurrentUserId, acl => acl.CanChangeProjectProperties);
      return View(new CloseProjectViewModel()
      {
        OriginalName = project.ProjectName,
        ProjectId = projectid,
        PublishPlot = isMaster,
        IsMaster = isMaster,
      });
    }

    [HttpPost, Authorize]
    public async Task<ActionResult> Close(CloseProjectViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var errorResult = await AsMasterOrAdmin(project, pacl => pacl.CanChangeProjectProperties);
      if (errorResult != null)
      {
        return errorResult;
      }
      viewModel.OriginalName = project.ProjectName;
      viewModel.IsMaster = project.HasMasterAccess(CurrentUserId, acl => acl.CanChangeProjectProperties);
      if (!ModelState.IsValid)
      {
        return View(viewModel);
      }
      try
      {
        await ProjectService.CloseProject(viewModel.ProjectId, CurrentUserId, viewModel.PublishPlot);
        return await RedirectToProject(viewModel.ProjectId);
      }
      catch (Exception ex)
      {
        ModelState.AddException(ex);
        return View(viewModel);
      }
    }
  }
}