using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Masters;
using JoinRpg.WebPortal.Models.Masters;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[RequireMasterOrAdmin()]
[Route("{projectId}/masters")]
public class AclController(
    IProjectMetadataRepository projectMetadataRepository,
    IClaimsRepository claimRepository,
    IUserRepository userRepository,
    IProjectAccessService projectAccessService,
    ICurrentUserAccessor currentUserAccessor
    ) : JoinControllerGameBase
{
    [HttpGet("add/{userId}")]
    [MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Add(ProjectIdentification projectId, UserIdentification userId) => await ShowAddPage(projectId, userId);

    private async Task<ActionResult> ShowAddPage(ProjectIdentification projectId, UserIdentification userId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        var targetUser = await userRepository.GetUserInfo(userId);

        if (targetUser is null)
        {
            return NotFound();
        }

        return View(new AclViewModel(project, targetUser, currentUserAccessor));
    }

    [HttpPost("add/{userId}")]
    [MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Add(ChangeAclViewModel viewModel)
    {
        try
        {
            await projectAccessService.GrantAccess(new GrantAccessRequest()
            {
                ProjectId = new ProjectIdentification(viewModel.ProjectId),
                UserId = new UserIdentification(viewModel.UserId),
                Permissions = viewModel.ToPermissions(),
            });
        }
        catch (Exception exception)
        {
            AddModelException(exception);
            return await ShowAddPage(new(viewModel.ProjectId), new UserIdentification(viewModel.UserId));
        }

        return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });
    }

    [RequireMasterOrAdmin()]
    [HttpGet]
    public async Task<ActionResult> Index(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var claims = await claimRepository.GetClaimsCountByMasters(projectId, ClaimStatusSpec.Active);

        return View(new MastersListViewModel(claims, currentUserAccessor, projectInfo));
    }

    [HttpGet("delete")]
    [MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Delete(ProjectIdentification projectId, UserIdentification userId)
    {
        AclViewModel? innerModel = await GetAclViewModel(projectId, userId);
        if (innerModel is null)
        {
            return NotFound();
        }

        var viewModel = new DeleteAclViewModel()
        {
            InnerModel = innerModel,
            ProjectId = projectId,
            ResponsibleMasterId = null,
            SelfRemove = userId == currentUserAccessor.UserId,
            UserId = userId,
        };
        return View("Delete", viewModel);
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken, MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Delete(DeleteAclViewModel viewModel)
    {
        ProjectIdentification projectId = new(viewModel.ProjectId);
        try
        {
            await projectAccessService.RemoveAccess(
                projectId,
                new UserIdentification(viewModel.UserId),
                viewModel.ResponsibleMasterId is int responsibleMasterId ? new UserIdentification(responsibleMasterId) : null);
        }
        catch
        {
            if (await GetAclViewModel(projectId, new(viewModel.UserId)) is not { } aclViewModel)
            {
                return NotFound();
            }
            viewModel.InnerModel = aclViewModel;
            return View(viewModel);
        }
        if (viewModel.UserId == currentUserAccessor.UserId)
        {
            //We are removing ourself, need to redirect to public page
            return RedirectToIndex(projectId);
        }
        return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });
    }

    [HttpGet("leave")]
    [RequireMaster()]
    public async Task<ActionResult> RemoveYourself(ProjectIdentification projectId, UserIdentification userId) => await Delete(projectId, userId);

    [HttpPost("leave")]
    [ValidateAntiForgeryToken, RequireMaster()]
    public async Task<ActionResult> RemoveYourself(DeleteAclViewModel viewModel) => await Delete(viewModel);

    [HttpGet("edit")]
    [MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Edit(ProjectIdentification projectId, UserIdentification userId)
    {
        var model = await GetAclViewModel(projectId, userId);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken, MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Edit(ChangeAclViewModel viewModel)
    {
        try
        {
            await projectAccessService.ChangeAccess(new ChangeAccessRequest()
            {
                ProjectId = new ProjectIdentification(viewModel.ProjectId),
                UserId = new UserIdentification(viewModel.UserId),
                Permissions = viewModel.ToPermissions(),
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
        await projectAccessService.GrantFullAccess(new(projectId));
        return RedirectToAction("Details", "Game", new { projectId });
    }

    private async Task<AclViewModel?> GetAclViewModel(ProjectIdentification projectId, UserIdentification userId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var master = projectInfo.Masters.SingleOrDefault(m => m.UserId == userId);
        if (master is null)
        {
            return null;
        }
        var claims = await claimRepository.GetClaimsForMaster(projectId, userId, ClaimStatusSpec.Any);
        return new AclViewModel(master, claims.Count, projectInfo);
    }
}
