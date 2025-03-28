using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain.Problems;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models.CheckIn;
using JoinRpg.Web.Models.Masters;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/checkin/[action]")]
[MasterAuthorize()] //TODO specific permission
public class CheckInController : ControllerGameBase
{
    private readonly IProblemValidator<Claim> claimValidator;
    private readonly IProjectMetadataRepository projectMetadataRepository;

    private IClaimsRepository ClaimsRepository { get; }

    private IPlotRepository PlotRepository { get; }

    private IClaimService ClaimService { get; }

    private ICharacterRepository CharacterRepository { get; }

    public CheckInController(
        IProjectRepository projectRepository,
        IProjectService projectService,
        IClaimsRepository claimsRepository,
        IPlotRepository plotRepository,
        IClaimService claimService,
        ICharacterRepository characterRepository,
        IUserRepository userRepository,
        IProjectMetadataRepository projectMetadataRepository,
        IProblemValidator<Claim> claimValidator)
      : base(projectRepository, projectService, userRepository)
    {
        ClaimsRepository = claimsRepository;
        PlotRepository = plotRepository;
        ClaimService = claimService;
        CharacterRepository = characterRepository;
        this.projectMetadataRepository = projectMetadataRepository;
        this.claimValidator = claimValidator;
    }

    [HttpGet]
    public async Task<ActionResult> Index(int projectId)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);
        if (!project.Details.EnableCheckInModule || !project.Details.CheckInProgress)
        {
            return View("CheckInNotStarted");
        }
        var claims = await ClaimsRepository.GetClaimHeadersWithPlayer(projectId, ClaimStatusSpec.ReadyForCheckIn);
        return View(new CheckInIndexViewModel(project, claims));
    }

    [HttpPost]
    public ActionResult Index(int projectId, int claimId) => RedirectToAction("CheckIn", new { projectId, claimId });

    [HttpGet, MasterAuthorize(Permission.CanChangeProjectProperties)]
    public async Task<ActionResult> Setup(int projectId)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);
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
            await ProjectService.SetCheckInOptions(model.ProjectId, model.CheckInProgress,
              model.EnableCheckInModule, model.AllowSecondRoles);
            return RedirectToAction("Setup", new { model.ProjectId });
        }
        catch (Exception ex)
        {
            ModelState.AddException(ex);
            return View(model);
        }
    }

    [HttpGet("~/{ProjectId}/claim/{ClaimId}/checkin")]
    public async Task<ActionResult> CheckIn(int projectId, int claimId)
    {
        var claim = await ClaimsRepository.GetClaimWithDetails(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }
        return await ShowCheckInForm(claim);
    }

    private async Task<ActionResult> ShowCheckInForm(Claim claim)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(claim.ProjectId));

        return View("CheckIn", new CheckInClaimModel(claim, await GetCurrentUserAsync(),
          claim.Character == null
            ? null
            : await PlotRepository.GetPlotsForCharacter(claim.Character),
          claimValidator,
          projectInfo
          ));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> DoCheckIn(int projectId, int claimId, int money, Checkbox? feeAccepted)
    {
        var claim = await ClaimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }
        try
        {
            await ClaimService.CheckInClaim(projectId, claimId, feeAccepted == Checkbox.@on ? money : 0);
            return RedirectToAction("Index", new { projectId });
        }
        catch (Exception ex)
        {
            ModelState.AddException(ex);
            return await ShowCheckInForm(claim);
        }
    }

    public enum Checkbox
    {
        on = 1,
        off = 0,
    }

    [HttpGet("~/{ProjectId}/claim/{ClaimId}/secondrole")]
    public async Task<ActionResult> SecondRole(int projectId, int claimId) => await ShowSecondRole(projectId, claimId);

    private async Task<ActionResult> ShowSecondRole(int projectId, int claimId)
    {
        var claim = await ClaimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }
        if (claim.ClaimStatus != Claim.Status.CheckedIn)
        {
            return RedirectToAction("Edit", "Claim", new { projectId, claimId });
        }

        var characters = await CharacterRepository.GetAvailableCharacters(projectId);

        return View(new SecondRoleViewModel(claim, characters, await GetCurrentUserAsync()));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("~/{ProjectId}/claim/{ClaimId}/secondrole")]
    public async Task<ActionResult> SecondRole(SecondRoleViewModel model)
    {
        var claim = await ClaimsRepository.GetClaim(model.ProjectId, model.ClaimId);
        if (claim == null)
        {
            return NotFound();
        }
        try
        {
            var newClaim = await ClaimService.MoveToSecondRole(model.ProjectId, model.ClaimId, model.CharacterId);
            return RedirectToAction("CheckIn", new { model.ProjectId, claimId = newClaim });
        }
        catch (Exception ex)
        {
            ModelState.AddException(ex);
            return await ShowSecondRole(model.ProjectId, model.ClaimId);
        }
    }

    [HttpGet]
    public ActionResult Stat(int projectid)
    {
        ViewBag.ProjectId = projectid;
        return View();
    }
}
