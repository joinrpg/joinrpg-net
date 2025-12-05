using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Masters;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[RequireMasterOrAdmin()]
[Route("{projectId}/masters")]
public class AclController(
    IProjectRepository projectRepository,
    IProjectMetadataRepository projectMetadataRepository,
    IProjectService projectService,
    IClaimsRepository claimRepository,
    IUriService uriService,
    IUserRepository userRepository,
    IResponsibleMasterRulesRepository responsibleMasterRulesRepository,
    IProjectAccessService projectAccessService,
    ICurrentUserAccessor currentUserAccessor
    ) : ControllerGameBase(projectRepository, projectService)
{
    protected readonly IUserRepository UserRepository = userRepository;

    [HttpGet("add/{userId}")]
    [MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Add(ProjectIdentification projectId, int userId) => await ShowAddPage(projectId, userId);

    private async Task<ActionResult> ShowAddPage(ProjectIdentification projectId, int userId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        var targetUser = await UserRepository.GetById(userId);

        return View(new AclViewModel(project, targetUser, PermissionExtensions.GetEmptyPermissionViewModels(project)));
    }

    [HttpPost("add/{userId}")]
    [MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Add(ChangeAclViewModel viewModel)
    {
        try
        {
            await projectAccessService.GrantAccess(new GrantAccessRequest()
            {
                ProjectId = viewModel.ProjectId,
                UserId = viewModel.UserId,
                CanGrantRights = viewModel.CanGrantRights,
                CanChangeFields = viewModel.CanChangeFields,
                CanChangeProjectProperties = viewModel.CanChangeProjectProperties,
                CanManageClaims = viewModel.CanManageClaims,
                CanEditRoles = viewModel.CanEditRoles,
                CanManageMoney = viewModel.CanManageMoney,
                CanSendMassMails = viewModel.CanSendMassMails,
                CanManagePlots = viewModel.CanManagePlots,
                CanManageAccommodation = viewModel.CanManageAccommodation,
                CanSetPlayersAccommodations = viewModel.CanSetPlayersAccommodations,
            });
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            return await ShowAddPage(new(viewModel.ProjectId), viewModel.UserId);
        }

        return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });
    }

    [RequireMasterOrAdmin()]
    [HttpGet]
    public async Task<ActionResult> Index(ProjectIdentification projectId)
    {
        var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var claims = await claimRepository.GetClaimsCountByMasters(projectId, ClaimStatusSpec.Active);
        var groups = await responsibleMasterRulesRepository.GetResponsibleMasterRules(projectId);

        return View(new MastersListViewModel(project, claims, groups, currentUserAccessor, uriService, projectInfo));
    }

    [HttpGet("delete")]
    [MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Delete(ProjectIdentification projectId, int projectaclid)
    {
        AclViewModel innerModel = await GetAclViewModel(projectId, projectaclid);

        var viewModel = new DeleteAclViewModel()
        {
            InnerModel = innerModel,
            ProjectAclId = projectaclid,
            ProjectId = projectId,
            ResponsibleMasterId = null,
            SelfRemove = innerModel.UserId == CurrentUserId,
            UserId = innerModel.UserId,
        };
        return View("Delete", viewModel);

    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken, MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Delete(DeleteAclViewModel viewModel)
    {
        try
        {
            await projectAccessService.RemoveAccess(viewModel.ProjectId, viewModel.UserId, viewModel.ResponsibleMasterId);
        }
        catch
        {
            viewModel.InnerModel = await GetAclViewModel(new(viewModel.ProjectId), viewModel.ProjectAclId);
            return View(viewModel);
        }
        if (viewModel.UserId == CurrentUserId)
        {
            //We are removing ourself, need to redirect to public page
            return await RedirectToProject(viewModel.ProjectId);
        }
        return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });

    }

    [HttpGet("leave")]
    [RequireMaster()]
    public async Task<ActionResult> RemoveYourself(ProjectIdentification projectId, int projectAclId) => await Delete(projectId, projectAclId);

    [HttpPost("leave")]
    [ValidateAntiForgeryToken, RequireMaster()]
    public async Task<ActionResult> RemoveYourself(DeleteAclViewModel viewModel) => await Delete(viewModel);


    [HttpGet("edit")]
    [MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Edit(ProjectIdentification projectId, int projectaclid)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);
        var projectAcl = project.ProjectAcls.Single(acl => acl.ProjectAclId == projectaclid);

        var groups = await responsibleMasterRulesRepository.GetResponsibleMasterRulesForMaster(projectId, new(projectAcl.UserId));

        return View(await GetAclViewModel(projectId, projectaclid));
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken, MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Edit(ChangeAclViewModel viewModel)
    {
        try
        {
            await projectAccessService.ChangeAccess(new ChangeAccessRequest()
            {
                ProjectId = viewModel.ProjectId,
                UserId = viewModel.UserId,
                CanGrantRights = viewModel.CanGrantRights,
                CanChangeFields = viewModel.CanChangeFields,
                CanChangeProjectProperties = viewModel.CanChangeProjectProperties,
                CanManageClaims = viewModel.CanManageClaims,
                CanEditRoles = viewModel.CanEditRoles,
                CanManageMoney = viewModel.CanManageMoney,
                CanSendMassMails = viewModel.CanSendMassMails,
                CanManagePlots = viewModel.CanManagePlots,
                CanManageAccommodation = viewModel.CanManageAccommodation,
                CanSetPlayersAccommodations = viewModel.CanSetPlayersAccommodations,
            });
        }
        catch
        {
            //TODO Fix this
            return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });
        }
        return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });

    }

    [AdminAuthorize]
    [HttpGet("force-admin-access")]
    public ActionResult ForceSet(int projectid) => View();

    [AdminAuthorize]
    [HttpPost("force-admin-access")]
    public async Task<ActionResult> ForceSet(int projectId, IFormCollection unused)
    {
        await ProjectService.GrantAccessAsAdmin(projectId);
        return RedirectToAction("Details", "Game", new { projectId });
    }

    private async Task<AclViewModel> GetAclViewModel(ProjectIdentification projectId, int projectaclid)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var projectAcl = project.ProjectAcls.Single(acl => acl.ProjectAclId == projectaclid);
        var claims = await claimRepository.GetClaimsForMaster(projectId, projectAcl.UserId, ClaimStatusSpec.Any);
        var groups = await responsibleMasterRulesRepository.GetResponsibleMasterRulesForMaster(new(projectId), new(projectAcl.UserId));
        var innerModel = new AclViewModel(projectAcl, claims.Count, groups, uriService, projectInfo);
        return innerModel;
    }
}
