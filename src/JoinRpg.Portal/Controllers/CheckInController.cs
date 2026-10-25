using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models.CheckIn;
using JoinRpg.Web.UserProfile;
using JoinRpg.WebPortal.Managers.CheckIn;
using JoinRpg.WebPortal.Managers.Plots;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/checkin/[action]")]
[MasterAuthorize()] //TODO specific permission
public class CheckInController(
    IProjectService projectService,
    IClaimsRepository claimsRepository,
    IClaimInfoRepository claimInfoRepository,
    IClaimService claimService,
    CheckInDeskManager checkInDeskManager,
    IUserRepository userRepository,
    IProjectMetadataRepository projectMetadataRepository,
    IClaimProblemValidator claimValidator,
    CharacterPlotViewService characterPlotViewService,
    ICurrentUserAccessor currentUserAccessor,
    UserProfileDetailsBuilder cardBuilder,
    IProjectBadgeTemplateRepository badgeTemplateRepository
        ) : JoinControllerGameBase
{
    /// <param name="checkedInClaimId">Только что зарегистрированная заявка — показать баннер с печатью бейджа</param>
    // Имя метода другое: та же сигнатура уже у POST Index (выбор заявки).
    [HttpGet, ActionName("Index")]
    public async Task<ActionResult> IndexPage(ProjectIdentification projectId, ClaimIdentification? checkedInClaimId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (!project.ProjectCheckInSettings.CheckInModuleEnabled || !project.ProjectCheckInSettings.InProgress)
        {
            return View("CheckInNotStarted");
        }

        CheckedInBannerViewModel? banner = null;
        // Параметр остаётся в адресе (история, F5, правка руками) — поэтому «зарегистрирован» показываем,
        // только если заявка и правда зарегистрирована.
        if (checkedInClaimId is not null
            && await claimInfoRepository.GetClaimInfoOrDefault(checkedInClaimId) is { } claimInfo
            && claimInfo.Claim.Status == ClaimStatus.CheckedIn)
        {
            var hasBadgeTemplates = (await badgeTemplateRepository.GetForProjectAsync(projectId)).Any(t => t.IsActive);
            banner = new CheckedInBannerViewModel(
                claimInfo.Player.DisplayName.DisplayName,
                claimInfo.Character.CharacterName,
                claimInfo.Character.Id,
                hasBadgeTemplates);
        }

        return View(new CheckInIndexViewModel(project, banner));
    }

    // claimId может не прийти вовсе: если заявок, готовых к регистрации, нет, селектор рендерит
    // <select> без опций, и браузер не отправляет поле. Тогда просто возвращаемся на страницу выбора.
    [HttpPost]
    public ActionResult Index(ProjectIdentification projectId, ClaimIdentification? claimId)
        => claimId is null
            ? RedirectToAction("Index", new { projectId = projectId.Value })
            : RedirectToAction("CheckIn", new { projectId = claimId.ProjectId.Value, claimId = claimId.ClaimId });

    [HttpGet, MasterAuthorize(Permission.CanChangeProjectProperties)]
    public async Task<ActionResult> Setup(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        return View(new CheckInSetupModel(project));
    }

    [HttpPost, MasterAuthorize(Permission.CanChangeProjectProperties)]
    public async Task<ActionResult> Setup(CheckInSetupModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        try
        {
            await projectService.SetCheckInSettings(new(model.ProjectId), model.CheckInProgress,
              model.EnableCheckInModule, model.AllowSecondRoles);
            return RedirectToAction("Setup", new { model.ProjectId });
        }
        catch (Exception ex)
        {
            AddModelException(ex);
            return View(model);
        }
    }

    [HttpGet("~/{ProjectId}/claim/{ClaimId}/checkin")]
    public async Task<ActionResult> CheckIn(ClaimIdentification claimId)
    {
        var claimInfo = await claimInfoRepository.GetClaimInfoOrDefault(claimId);

        return claimInfo is null ? NotFound() : await ShowCheckInForm(claimInfo);
    }

    private async Task<ActionResult> ShowCheckInForm(ClaimInfo claimInfo)
    {
        var characterId = claimInfo.Character.Id;
        var handouts = await characterPlotViewService.GetHandoutsForCharacters([characterId]);

        var model = new CheckInClaimModel(
            claimInfo,
            handouts[characterId],
            claimValidator,
            currentUserAccessor,
            cardBuilder
          );
        return View("CheckIn", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> DoCheckIn(ProjectIdentification projectId, int claimId, int money, Checkbox? feeAccepted)
    {
        var claimIdentification = new ClaimIdentification(projectId, claimId);
        if (await claimInfoRepository.GetClaimInfoOrDefault(claimIdentification) is null)
        {
            return NotFound();
        }
        try
        {
            await checkInDeskManager.CheckIn(claimIdentification, feeAccepted == Checkbox.@on ? money : 0);
            return RedirectToAction("Index", new { ProjectId = projectId.Value, checkedInClaimId = claimIdentification.ClaimId });
        }
        catch (Exception ex)
        {
            AddModelException(ex);
            // Перечитываем: взнос мог быть уже принят, хотя регистрация не прошла (#4890), — тогда
            // форма должна показать, что долга больше нет.
            var claimInfo = await claimInfoRepository.GetClaimInfoOrDefault(claimIdentification);
            return claimInfo is null ? NotFound() : await ShowCheckInForm(claimInfo);
        }
    }

    public enum Checkbox
    {
        on = 1,
        off = 0,
    }

    [HttpGet("~/{ProjectId}/claim/{ClaimId}/secondrole")]
    public async Task<ActionResult> SecondRole(ProjectIdentification projectId, int claimId) => await ShowSecondRole(projectId, claimId);

    private async Task<ActionResult> ShowSecondRole(ProjectIdentification projectId, int claimId)
    {
        var claimInfo = await claimInfoRepository.GetClaimInfoOrDefault(new ClaimIdentification(projectId, claimId));

        if (claimInfo is null)
        {
            return NotFound();
        }
        if (claimInfo.Claim.Status != ClaimStatus.CheckedIn)
        {
            return RedirectToAction("Edit", "Claim", new { projectId, claimId });
        }

        var model = new SecondRoleViewModel(claimInfo, currentUserAccessor, cardBuilder);
        return View(model);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("~/{ProjectId}/claim/{ClaimId}/secondrole")]
    public async Task<ActionResult> SecondRole(SecondRoleViewModel model)
    {
        var claim = await claimsRepository.GetClaim(new ClaimIdentification(model.ProjectId, model.ClaimId));
        if (claim == null)
        {
            return NotFound();
        }
        try
        {
            var newClaim = await claimService.MoveToSecondRole(claim.GetId(), model.CharacterId, ".");
            return RedirectToAction("CheckIn", new { model.ProjectId, claimId = newClaim });
        }
        catch (Exception ex)
        {
            AddModelException(ex);
            return await ShowSecondRole(new(model.ProjectId), model.ClaimId);
        }
    }

    [HttpGet]
    public ActionResult Stat(int projectid)
    {
        ViewBag.ProjectId = projectid;
        return View();
    }
}
