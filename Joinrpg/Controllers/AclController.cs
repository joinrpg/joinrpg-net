using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  [MasterAuthorize(AllowAdmin = true)]
  public class AclController : ControllerGameBase
  { 
    private IClaimsRepository ClaimRepository { get; }
    [HttpPost, ValidateAntiForgeryToken, MasterAuthorize(Permission.CanGrantRights) ]
    public async Task<ActionResult> Add(AclViewModel viewModel)
    {
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
      var claims = await ClaimRepository.GetClaimsCountByMasters(projectId, ClaimStatusSpec.Active);
      var groups = await ProjectRepository.GetGroupsWithResponsible(projectId);
      var currentUser = await GetCurrentUserAsync();

      return View(project.ProjectAcls.Select(acl =>
      {  
        return AclViewModel.FromAcl(acl, claims.SingleOrDefault(c => c.MasterId == acl.UserId)?.ClaimCount ?? 0,
          groups.Where(gr => gr.ResponsibleMasterUserId == acl.UserId && gr.IsActive).ToList(), currentUser);
      }));
    }

    [HttpGet, MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Delete(int projectId, int projectaclid)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      var projectAcl = project.ProjectAcls.Single(acl => acl.ProjectAclId == projectaclid);
      var claims = await ClaimRepository.GetClaimsForMaster(projectId, projectAcl.UserId,  ClaimStatusSpec.Any);
      var groups = await ProjectRepository.GetGroupsWithResponsible(projectId);
      return View(DeleteAclViewModel.FromAcl(projectAcl,
        claims.Count,
        groups.Where(gr => gr.ResponsibleMasterUserId == projectAcl.UserId).ToList()));

    }

    [HttpPost, ValidateAntiForgeryToken, MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Delete(DeleteAclViewModel viewModel)
    {
      try
      {
        await ProjectService.RemoveAccess(viewModel.ProjectId, viewModel.UserId, viewModel.ResponsibleMasterId);
      }
      catch
      {
        return View(viewModel);
      }
      if (viewModel.UserId == CurrentUserId)
      {
        //We are removing ourself, need to redirect to public page
        return await RedirectToProject(viewModel.ProjectId);
      }
      return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });

    }


    [HttpGet, MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Edit(int projectId, int? projectaclid)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      var groups = await ProjectRepository.GetGroupsWithResponsible(projectId);
      var projectAcl = project.ProjectAcls.Single(acl => acl.ProjectAclId == projectaclid);
      var currentUser = await GetCurrentUserAsync();
      return View(AclViewModel.FromAcl(projectAcl, 0,
        groups.Where(gr => gr.ResponsibleMasterUserId == projectAcl.UserId).ToList(), currentUser));
    }

    [HttpPost, ValidateAntiForgeryToken, MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Edit(AclViewModel viewModel)
    {
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

      [AdminAuthorize]
      [HttpGet]
      public ActionResult ForceSet(int projectid)
      {
          return View();
      }

      [AdminAuthorize]
      [HttpPost]
      public async Task<ActionResult> ForceSet(int projectId, [UsedImplicitly] FormCollection unused)
      {
          await ProjectService.GrantAccessAsAdmin(projectId);
          return RedirectToAction("Details", "Game", new {projectId});
      }
    }
}
