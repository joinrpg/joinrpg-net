using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models.CheckIn;
using JoinRpg.WebPortal.Managers.Plots;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/checkin/[action]")]
[MasterAuthorize()] //TODO specific permission
public class CheckInController(
    IProjectService projectService,
    IClaimsRepository claimsRepository,
    ICharacterInfoRepository characterInfoRepository,
    IClaimService claimService,
    IUserRepository userRepository,
    IProjectMetadataRepository projectMetadataRepository,
    IClaimProblemValidator claimValidator,
    CharacterPlotViewService characterPlotViewService,
    ICurrentUserAccessor currentUserAccessor
        ) : JoinControllerGameBase
{
    [HttpGet]
    public async Task<ActionResult> Index(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (!project.ProjectCheckInSettings.CheckInModuleEnabled || !project.ProjectCheckInSettings.InProgress)
        {
            return View("CheckInNotStarted");
        }
        return View(new CheckInIndexViewModel(project));
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
        var characterInfo = await LoadCharacterByClaim(claimId);

        return characterInfo is null ? NotFound() : await ShowCheckInForm(characterInfo, claimId);
    }

    /// <summary>
    /// Персонаж заявки одним запросом. Всё, что странице нужно знать о заявке, лежит в агрегате
    /// (ADR013): и идентификатор персонажа, и игрок, и статус — поэтому EF-сущность здесь больше
    /// не грузится.
    /// </summary>
    private async Task<CharacterInfo?> LoadCharacterByClaim(ClaimIdentification claimId)
        => (await characterInfoRepository.GetCharacterInfosByClaims([claimId])).SingleOrDefault();

    private async Task<ActionResult> ShowCheckInForm(CharacterInfo characterInfo, ClaimIdentification claimId)
    {
        var characterId = characterInfo.Id;
        var handouts = await characterPlotViewService.GetHandoutsForCharacters([characterId]);

        return View("CheckIn",
            new CheckInClaimModel(claimId,
            characterInfo,
            await userRepository.GetRequiredUserInfo(currentUserAccessor.UserIdentification),
            await userRepository.GetRequiredUserInfo(characterInfo.GetClaimById(claimId).PlayerId),
            handouts[characterId],
            claimValidator,
            currentUserAccessor
          ));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> DoCheckIn(ProjectIdentification projectId, int claimId, int money, Checkbox? feeAccepted)
    {
        var claimIdentification = new ClaimIdentification(projectId, claimId);
        // Грузим заранее: нужен и для 404, и для того, чтобы показать ту же форму при ошибке
        // сохранения. Неудачная операция ничего не коммитит, поэтому снимок остаётся верным.
        var characterInfo = await LoadCharacterByClaim(claimIdentification);
        if (characterInfo is null)
        {
            return NotFound();
        }
        try
        {
            await claimService.CheckInClaim(claimIdentification, feeAccepted == Checkbox.@on ? money : 0);
            return RedirectToAction("Index", new { ProjectId = projectId.Value });
        }
        catch (Exception ex)
        {
            AddModelException(ex);
            return await ShowCheckInForm(characterInfo, claimIdentification);
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
        var claim = await claimsRepository.GetClaim(new ClaimIdentification(projectId, claimId));
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        if (claim == null)
        {
            return NotFound();
        }
        if (claim.ClaimStatus != ClaimStatus.CheckedIn)
        {
            return RedirectToAction("Edit", "Claim", new { projectId, claimId });
        }

        var playerUserInfo = await userRepository.GetRequiredUserInfo(claim.GetPlayerId());

        return View(new SecondRoleViewModel(
            claim,
            await characterInfoRepository.GetCharacterInfo(claim.GetCharacterId()),
            currentUserAccessor,
            projectInfo,
            playerUserInfo));
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
