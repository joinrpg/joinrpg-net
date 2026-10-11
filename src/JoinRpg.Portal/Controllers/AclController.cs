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
    public async Task<ActionResult> Add(ProjectIdentification projectId, UserIdentification userId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (project.Masters.Any(m => m.UserId == userId) || project.FormerMasters.Any(m => m.UserId == userId))
        {
            // Действующего мастера правят, бывшего возвращают правкой прав — обоих на странице правки (ADR019, §1).
            return RedirectToAction(nameof(Edit), new { projectId = projectId.Value, userId = userId.Value });
        }
        var targetUser = await userRepository.GetUserInfo(userId);
        // Форма — остров AddMasterPanel: профиль и права грузит и сохраняет через API (ADR019, §4).
        return targetUser is null ? NotFound() : View(new AclViewModel(project, targetUser, currentUserAccessor));
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

    // Свой профиль (роль, описание, публичность) мастер правит сам, чужой и права — с CanGrantRights (ADR019, §7).
    [HttpGet("edit")]
    [RequireMaster()]
    public async Task<ActionResult> Edit(ProjectIdentification projectId, UserIdentification userId)
    {
        var model = await GetEditViewModel(projectId, userId);
        return model is null ? NotFound() : View(model);
    }

    /// <summary>
    /// Модель страницы правки мастера: остров профиля и, если можно, остров прав.
    /// Чужого мастера без права выдавать доступ править нельзя.
    /// </summary>
    private async Task<AclViewModel?> GetEditViewModel(ProjectIdentification projectId, UserIdentification userId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var canGrantRights = projectInfo.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault, Permission.CanGrantRights);
        var model = await GetAclViewModel(projectId, userId);
        if (model is null)
        {
            // Бывший мастер: страница правки — это его возврат в проект, только с правом выдавать доступ (ADR019, §1).
            if (!canGrantRights
                || !projectInfo.FormerMasters.Any(m => m.UserId == userId)
                || await userRepository.GetUserInfo(userId) is not { } formerUser)
            {
                return null;
            }
            // Карточка — как на странице добавления: причина доступа к профилю считается по проекту,
            // и снятый мастер в ней уже не «Со-мастер».
            model = new AclViewModel(projectInfo, formerUser, currentUserAccessor) { IsFormerMaster = true };
        }
        // Права меняются в активном и заблокированном проекте (ChangeAccess — AllowBlocked, ADR023),
        // профиль — и в архиве: это «титры» игры.
        model.CanEditPermissions = canGrantRights && !projectInfo.IsArchived;
        if (!canGrantRights && userId != currentUserAccessor.UserIdentificationOrDefault)
        {
            _ = NoAccesToProjectView(projectInfo, currentUserAccessor); // бросает NoAccessToProjectException
        }
        return model;
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
