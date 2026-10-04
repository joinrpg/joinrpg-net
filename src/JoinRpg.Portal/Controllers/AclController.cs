using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Masters;
using JoinRpg.Web.ProjectCommon.Masters;
using JoinRpg.WebPortal.Models.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[RequireMasterOrAdmin()]
[Route("{projectId}/masters")]
public class AclController(
    IProjectMetadataRepository projectMetadataRepository,
    IClaimsRepository claimRepository,
    IUserRepository userRepository,
    IProjectAccessService projectAccessService,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMasterProfileRepository masterProfileRepository
    ) : JoinControllerGameBase
{
    [HttpGet("add/{userId}")]
    [MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Add(ProjectIdentification projectId, UserIdentification userId) => await ShowAddPage(projectId, userId);

    private async Task<ActionResult> ShowAddPage(ProjectIdentification projectId, UserIdentification userId, AddAclViewModel? posted = null)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        var targetUser = await userRepository.GetUserInfo(userId);

        if (targetUser is null)
        {
            return NotFound();
        }

        var model = new AclViewModel(project, targetUser, currentUserAccessor);
        if (posted is not null)
        {
            // Показываем форму с тем, что прислали.
            model.Role = posted.Role ?? "";
            model.Description = posted.Description;
            model.IsPublic = posted.IsPublic;
            var permissions = posted.ToPermissions();
            model.Badges = [.. model.Badges.Select(b => new PermissionBadgeViewModel(b.Permission, permissions.Contains(b.Permission)))];
        }
        else if (project.FormerMasters.SingleOrDefault(m => m.UserId == userId) is { } former
            && await masterProfileRepository.GetMasterProfile(projectId, userId) is { } formerProfile)
        {
            // Возвращаем бывшего мастера: форма предзаполнена его прежним профилем (ADR019, §1).
            model.Role = formerProfile.Role.Value;
            model.Description = formerProfile.Description?.Value;
            model.IsPublic = former.IsPublic;
        }
        return View(model);
    }

    [HttpPost("add/{userId}")]
    [MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Add(AddAclViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return await ShowAddPage(new(viewModel.ProjectId), new UserIdentification(viewModel.UserId), viewModel);
        }
        try
        {
            await projectAccessService.GrantAccess(new GrantAccessRequest()
            {
                ProjectId = new ProjectIdentification(viewModel.ProjectId),
                UserId = new UserIdentification(viewModel.UserId),
                Permissions = viewModel.ToPermissions(),
                Role = new(viewModel.Role!), // ModelState проверен выше — [Required]
                Description = MarkdownString.FromOptional(viewModel.Description),
                IsPublic = viewModel.IsPublic,
            });
        }
        catch (Exception exception)
        {
            AddModelException(exception);
            return await ShowAddPage(new(viewModel.ProjectId), new UserIdentification(viewModel.UserId), viewModel);
        }

        return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });
    }

    /// <summary>
    /// Список мастеров виден всем, включая анонимов, — как на главной странице проекта (ADR019).
    /// Права, заявки и управление — только мастерам и админу.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> Index(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var profiles = await masterProfileRepository.GetMasterProfiles(projectId);

        if (!projectInfo.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault) && !currentUserAccessor.IsAdmin)
        {
            var viewer = currentUserAccessor.UserIdentificationOrDefault;
            // Markdown рендерим только тем, кого покажем: непубличные всё равно отбросятся.
            var visible = projectInfo.GetMastersVisibleTo(viewer).Select(m => m.UserId).ToHashSet();
            var publicProfiles = profiles.Where(p => visible.Contains(p.UserId)).ToDictionary(
                p => p.UserId,
                p => new ProjectMasterProfileViewModel(p.Role.Value, p.Description is null ? null : p.Description.ToHtmlString()));
            return View("PublicIndex", ProjectMastersViewModel.Build(projectInfo, viewer, publicProfiles));
        }

        var claims = await claimRepository.GetClaimsCountByMasters(projectId, ClaimStatusSpec.Active);

        return View(new MastersListViewModel(claims, currentUserAccessor, projectInfo, profiles));
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
                viewModel.ResponsibleMasterId);
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
