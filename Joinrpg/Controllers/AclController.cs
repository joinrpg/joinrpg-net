﻿using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class AclController : ControllerGameBase
  { 
    private IClaimsRepository ClaimRepository { get; }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> Add(AclViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var error = await AsMasterOrAdmin(project, a => a.CanGrantRights);
      if (error != null) return error;

      try
      {
        await ProjectService.GrantAccess(viewModel.ProjectId, CurrentUserId, viewModel.UserId, viewModel.CanGrantRights,
          viewModel.CanChangeFields, viewModel.CanChangeProjectProperties, viewModel.CanManageClaims,
          viewModel.CanEditRoles, viewModel.CanManageMoney, viewModel.CanSendMassMails, viewModel.CanManagePlots);
      }
      catch
      {
        //TODO Fix this.
        ModelState.AddModelError("", "Error!");
        return RedirectToAction("Details", "User", new {viewModel.UserId});
      }
      return RedirectToAction("Index", "Acl", new {viewModel.ProjectId});
    }

    public AclController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService, IClaimsRepository claimRepository)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      ClaimRepository = claimRepository;
    }

    [HttpGet]
    public async Task<ActionResult> Index(int projectId)
    {
      var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
      var claims = await ClaimRepository.GetClaims(projectId, ClaimStatusSpec.Active);
      return AsMaster(project) ?? View(project.ProjectAcls.Select(acl =>
      {
        var result = AclViewModel.FromAcl(acl, claims.Count(c => c.ResponsibleMasterUserId == acl.UserId));
        result.ProblemClaimsCount =
          claims.Where(c => c.ResponsibleMasterUserId == acl.UserId)
            .Count(claim => claim.GetProblems(ProblemSeverity.Warning).Any());
        return result;
      }));
    }

    [HttpGet]
    public async Task<ActionResult> Delete(int projectid, int projectaclid)
    {
      var project = await ProjectRepository.GetProjectAsync(projectid);
      return AsMaster(project, acl => acl.CanGrantRights) ??
             View(AclViewModel.FromAcl(project.ProjectAcls.Single(acl => acl.ProjectAclId == projectaclid), 0));

    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(AclViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var error = AsMaster(project, acl => acl.CanGrantRights);
      if (error != null)
        return error;

      try
      {
        await ProjectService.RemoveAccess(viewModel.ProjectId, CurrentUserId, viewModel.UserId);
      }
      catch
      {
        return View(viewModel);
      }
      return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });

    }


    [HttpGet]
    public async Task<ActionResult> Edit(int projectid, int? projectaclid)
    {
      var project = await ProjectRepository.GetProjectAsync(projectid);
      return AsMaster(project, acl => acl.CanGrantRights) ??
             View(AclViewModel.FromAcl(project.ProjectAcls.Single(acl => acl.ProjectAclId == projectaclid), 0));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(AclViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var error = AsMaster(project, acl => acl.CanGrantRights);
      if (error != null)
        return error;

      try
      {
        await
          ProjectService.ChangeAccess(viewModel.ProjectId, CurrentUserId, viewModel.UserId, viewModel.CanGrantRights,
            viewModel.CanChangeFields, viewModel.CanChangeProjectProperties, viewModel.CanManageClaims,
            viewModel.CanEditRoles, viewModel.CanManageMoney, viewModel.CanSendMassMails, viewModel.CanManagePlots);
      }
      catch
      {
        return View(viewModel);
      }
      return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });

    }
  }
}