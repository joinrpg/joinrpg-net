using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models.CheckIn;
using JoinRpg.WebPortal.Managers.Plots;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/checkin/[action]")]
[MasterAuthorize()] //TODO specific permission
public class CheckInController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    IClaimsRepository claimsRepository,
    IClaimService claimService,
    ICharacterRepository characterRepository,
    IUserRepository userRepository,
    IProjectMetadataRepository projectMetadataRepository,
    IProblemValidator<Claim> claimValidator,
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

    [HttpPost]
    public ActionResult Index(int projectId, int claimId) => RedirectToAction("CheckIn", new { projectId, claimId });

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
    public async Task<ActionResult> CheckIn(int projectId, int claimId)
    {
        var claim = await claimsRepository.GetClaimWithDetails(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }
        return await ShowCheckInForm(claim);
    }

    private async Task<ActionResult> ShowCheckInForm(Claim claim)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(claim.ProjectId));

        var characterId = claim.GetCharacterId();
        var handouts = await characterPlotViewService.GetHandoutsForCharacters([characterId]);

        return View("CheckIn",
            new CheckInClaimModel(claim,
            await userRepository.GetById(currentUserAccessor.UserIdentification),
            handouts[characterId],
            claimValidator,
            projectInfo
          ));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> DoCheckIn(ProjectIdentification projectId, int claimId, int money, Checkbox? feeAccepted)
    {
        var claimIdentification = new ClaimIdentification(projectId, claimId);
        var claim = await claimsRepository.GetClaim(claimIdentification);
        if (claim == null)
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
            return await ShowCheckInForm(claim);
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
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (claim == null)
        {
            return NotFound();
        }
        if (claim.ClaimStatus != ClaimStatus.CheckedIn)
        {
            return RedirectToAction("Edit", "Claim", new { projectId, claimId });
        }

        var characters = await characterRepository.GetAvailableCharacters(projectId);

        return View(new SecondRoleViewModel(claim, characters, await userRepository.GetById(currentUserAccessor.UserIdentification), projectInfo));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("~/{ProjectId}/claim/{ClaimId}/secondrole")]
    public async Task<ActionResult> SecondRole(SecondRoleViewModel model)
    {
        var claim = await claimsRepository.GetClaim(model.ProjectId, model.ClaimId);
        if (claim == null)
        {
            return NotFound();
        }
        try
        {
            var newClaim = await claimService.MoveToSecondRole(claim.GetId(), new(model.ProjectId, model.CharacterId), ".");
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
