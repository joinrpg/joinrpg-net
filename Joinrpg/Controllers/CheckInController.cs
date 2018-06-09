using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CheckIn;

namespace JoinRpg.Web.Controllers
{
  [MasterAuthorize()] //TODO specific permission
  public class CheckInController : ControllerGameBase
  {
    [ProvidesContext]
    private IClaimsRepository ClaimsRepository { get; }

    [ProvidesContext]
    private IPlotRepository PlotRepository { get; }

    [ProvidesContext]
    private IClaimService ClaimService { get; }

    [ProvidesContext]
    private ICharacterRepository CharacterRepository { get; }

    public CheckInController(
      ApplicationUserManager userManager,
      [NotNull] IProjectRepository projectRepository, 
      IProjectService projectService,
      IExportDataService exportDataService,
      IClaimsRepository claimsRepository, 
      IPlotRepository plotRepository, 
      IClaimService claimService, 
      ICharacterRepository characterRepository) 
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      ClaimsRepository = claimsRepository;
      PlotRepository = plotRepository;
      ClaimService = claimService;
      CharacterRepository = characterRepository;
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
    public ActionResult Index(int projectId, int claimId)
    {
      return RedirectToAction("CheckIn", new {projectId, claimId});
    }

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
        return RedirectToAction("Setup", new { model.ProjectId});
      }
      catch (Exception ex)
      {
        ModelState.AddException(ex);
        return View(model);
      }
    }

    [HttpGet]
    public async Task<ActionResult> CheckIn(int projectId, int claimId)
    {
      var claim = await ClaimsRepository.GetClaimWithDetails(projectId, claimId);
      if (claim == null)
      {
        return HttpNotFound();
      }
      return await ShowCheckInForm(claim);
    }

    private async Task<ActionResult> ShowCheckInForm(Claim claim)
    {
      return View("CheckIn", new CheckInClaimModel(claim, await GetCurrentUserAsync(),
        claim.Character == null
          ? null
          : await PlotRepository.GetPlotsForCharacter(claim.Character)));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<ActionResult> DoCheckIn(int projectId, int claimId, int money, Checkbox? feeAccepted)
    {
      var claim = await ClaimsRepository.GetClaim(projectId, claimId);
      if (claim == null)
      {
        return HttpNotFound();
      }
      try
      {
          await ClaimService.CheckInClaim(projectId, claimId, feeAccepted == Checkbox.@on ? money : 0);
        return RedirectToAction("Index", new {projectId});
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

    [HttpGet]
    public async Task<ActionResult> SecondRole(int projectId, int claimId)
    {
      return await ShowSecondRole(projectId, claimId);
    }

    private async Task<ActionResult> ShowSecondRole(int projectId, int claimId)
    {
      var claim = await ClaimsRepository.GetClaim(projectId, claimId);
      if (claim == null)
      {
        return HttpNotFound();
      }
      if (claim.ClaimStatus != Claim.Status.CheckedIn)
      {
        return RedirectToAction("Edit", "Claim", new {projectId, claimId});
      }

      var characters = await CharacterRepository.GetAvailableCharacters(projectId);

      return View(new SecondRoleViewModel(claim, characters, await GetCurrentUserAsync()));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<ActionResult> SecondRole(SecondRoleViewModel model)
    {
      var claim = await ClaimsRepository.GetClaim(model.ProjectId, model.ClaimId);
      if (claim == null)
      {
        return HttpNotFound();
      }
      try
      {
        var newClaim = await ClaimService.MoveToSecondRole(model.ProjectId, model.ClaimId, model.CharacterId);
        return RedirectToAction("CheckIn", new { model.ProjectId, claimId = newClaim});
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
}