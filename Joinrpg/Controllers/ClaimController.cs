using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Controllers
{
  public class ClaimController : ControllerGameBase
  {
    private readonly IClaimService _claimService;
    private readonly IPlotRepository _plotRepository;
    private readonly IClaimsRepository _claimsRepository;
    private IFinanceService FinanceService { get; }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> AddForCharacter(int projectid, int characterid)
    {
      var field = await ProjectRepository.GetCharacterAsync(projectid, characterid);
      return WithEntity(field) ?? View("Add", AddClaimViewModel.Create(field, GetCurrentUser()));
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> AddForGroup(int projectid, int characterGroupId)
    {
      var field = await ProjectRepository.LoadGroupAsync(projectid, characterGroupId);
      return WithEntity(field.Project) ?? View("Add", AddClaimViewModel.Create(field, GetCurrentUser()));
    }

    public ClaimController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IClaimService claimService, IPlotRepository plotRepository,
      IClaimsRepository claimsRepository, IFinanceService financeService, IExportDataService exportDataService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      _claimService = claimService;
      _plotRepository = plotRepository;
      _claimsRepository = claimsRepository;
      FinanceService = financeService;
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Add(AddClaimViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var error = WithEntity(project);
      if (error != null)
      {
        return error;
      }

      try
      {
        await _claimService.AddClaimFromUser(viewModel.ProjectId, viewModel.CharacterGroupId, viewModel.CharacterId,
          CurrentUserId, viewModel.ClaimText.Contents);

        return RedirectToAction("My", "Claim");
      }
      catch
      {
        //TODO: Отображать ошибки верно
        return View(viewModel);
      }
    }

    [HttpGet, Authorize]
    public ActionResult My() => View(GetCurrentUser().Claims.Select(ClaimListItemViewModel.FromClaim));

    [HttpGet, Authorize]
    public Task<ActionResult> ForPlayer(int projectId, int userId, string export)
      => MasterList(projectId, cl => cl.IsActive & cl.PlayerUserId == userId, "ForPlayer", export);

    [HttpGet, Authorize]
    public Task<ActionResult> ListForGroupDirect(int projectId, int characterGroupId, string export)
    {
      ViewBag.CharacterGroupId = characterGroupId;
      return MasterList(projectId, cl => cl.IsActive && cl.CharacterGroupId == characterGroupId, "ListForGroupDirect", export);
    }

    public Task<ActionResult> ListForGroup(int projectId, int characterGroupId, string export)
    {
      ViewBag.CharacterGroupId = characterGroupId;
      return MasterList(projectId, cl => cl.IsActive && cl.PartOfGroup(characterGroupId), "ListForGroup", export);
    }

    public Task<ActionResult> DiscussingForGroup(int projectId, int characterGroupId, string export)
    {
      ViewBag.CharacterGroupId = characterGroupId;
      return MasterList(projectId, cl => cl.IsInDiscussion && cl.PartOfGroup(characterGroupId), "DiscussingForGroup", export);
    }

    public Task<ActionResult> DiscussingForGroupDirect(int projectId, int characterGroupId, string export)
    {
      ViewBag.CharacterGroupId = characterGroupId;
      return MasterList(projectId, cl => cl.IsInDiscussion && cl.CharacterGroupId == characterGroupId, "DiscussingForGroupDirect", export);
    }

    private async Task<ActionResult> MasterList(int projectId, Func<Claim, bool> predicate, [AspMvcView] string viewName,
      string export)
    {
      var claims = await _claimsRepository.GetClaims(projectId);

      var error = await AsMaster(claims, projectId);

      if (error != null) return error;

      var viewModel = claims.Where(predicate).Select(
        claim => ClaimListItemViewModel.FromClaim(claim, CurrentUserId)).ToList();

      ViewBag.ClaimIds = viewModel.Select(c => c.ClaimId).ToArray();
      ViewBag.ProjectId = projectId;
      var exportType = GetExportTypeByName(export);

      if (exportType == null)
      {
        return View(viewName, viewModel);
      }
      else
      {
        return await Export(viewModel, "claims", exportType.Value);
      }
    }

    [HttpGet, Authorize]
    public Task<ActionResult> Discussing(int projectid, string export)
      => MasterList(projectid, claim => claim.IsInDiscussion, "Discussing", export);

    [HttpGet, Authorize]
    public Task<ActionResult> WaitingForFee(int projectid, string export)
      => MasterList(projectid, claim => claim.IsApproved && !claim.ClaimPaidInFull(), "WaitingForFee", export);

    [HttpGet, Authorize]
    public Task<ActionResult> Responsible(int projectid, int responsibleMasterId, string export)
      =>
        MasterList(projectid, claim => claim.ResponsibleMasterUserId == responsibleMasterId && claim.IsActive,
          "Responsible", export);

    [HttpGet, Authorize]
    public async Task<ActionResult> Problems(int projectId, string export)
    {
      return await ShowProblems(projectId, export, await _claimsRepository.GetClaims(projectId));
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> ResponsibleProblems(int projectId, int responsibleMasterId, string export)
    {
      return await ShowProblems(projectId, export, await _claimsRepository.GetActiveClaimsForMaster(projectId, responsibleMasterId));
    }

    private async Task<ActionResult> ShowProblems(int projectId, string export, ICollection<Claim> claims)
    {
      var error = await AsMaster(claims, projectId);
      if (error != null)
        return error;

      var viewModel =
        claims.Select(c => ClaimProblemListItemViewModel.FromClaimProblem(c.GetProblems(), CurrentUserId, c))
          .Where(vm => vm.Problems.Any())
          .ToList();

      ViewBag.ClaimIds = viewModel.Select(c => c.ClaimId).ToArray();
      ViewBag.ProjectId = projectId;

      var exportType = GetExportTypeByName(export);

      if (exportType == null)
      {
        return View("Problems", viewModel);
      }

      return await Export(viewModel, "problem-claims", exportType.Value);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Edit(int projectId, int claimId)
    {
      var claim = await ProjectRepository.GetClaimWithDetails(projectId, claimId);
      var error = WithClaim(claim);
      if (error != null)
      {
        return error;
      }
      var hasMasterAccess = claim.Project.HasMasterAccess(CurrentUserId);
      var isMyClaim = claim.PlayerUserId == CurrentUserId;
      var hasPlayerAccess = isMyClaim && claim.IsApproved;
      var claimViewModel = new ClaimViewModel()
      {
        ClaimId = claim.ClaimId,
        Comments = claim.Comments.Where(comment => comment.ParentCommentId == null).Select(comment => new CommentViewModel(comment, CurrentUserId)),
        HasMasterAccess = hasMasterAccess,
        HasApproveRejectClaim = claim.HasMasterAccess(CurrentUserId, acl => acl.CanApproveClaims),
        IsMyClaim = isMyClaim,
        Player = claim.Player,
        ProjectId = claim.ProjectId,
        Status = claim.ClaimStatus,
        CharacterGroupId = claim.CharacterGroupId,
        GroupName = claim.Group?.CharacterGroupName,
        CharacterId = claim.CharacterId,
        OtherClaimsForThisCharacterCount = claim.IsApproved ? 0 : claim.OtherClaimsForThisCharacter().Count(),
        HasOtherApprovedClaim = !claim.IsApproved && claim.OtherClaimsForThisCharacter().Any(c => c.IsApproved),
        Data = CharacterGroupListViewModel.FromGroupAsMaster(claim.Project.RootGroup),
        OtherClaimsFromThisPlayerCount = claim.IsApproved ? 0 : claim.OtherActiveClaimsForThisPlayer().Count(),
        Description = new MarkdownViewModel(claim.Character?.Description),
        Masters =
          MasterListItemViewModel.FromProject(claim.Project)
            .Union(new MasterListItemViewModel() {Id = "-1", Name = "Нет"}),
        ResponsibleMasterId = claim.ResponsibleMasterUserId ?? -1,
        Fields = new CharacterFieldsViewModel()
        {
          CharacterFields = claim.Character?.Fields().Select(pair => pair.Value) ?? new CharacterFieldValue[] { },
          HasMasterAccess = hasMasterAccess,
          EditAllowed = true,
          HasPlayerAccessToCharacter = hasPlayerAccess
        },
        Navigation = CharacterNavigationViewModel.FromClaim(claim, CurrentUserId, CharacterNavigationPage.Claim),
        ClaimFee = new ClaimFeeViewModel()
        {
          CurrentTotalFee = claim.ClaimTotalFee(),
          CurrentBalance = claim.ClaimBalance(),
          CurrentFee = claim.ClaimCurrentFee()
        },
        Problems = claim.GetProblems().Select(p => new ProblemViewModel(p)).ToList(),
        PlayerDetails = UserProfileDetailsViewModel.FromUser(claim.Player, GetCurrentUser())
      };

      if (claimViewModel.Comments.Any(c => !c.IsRead))
      {
            _claimService.UpdateReadCommentWatermark(claim.ProjectId, claim.ClaimId, CurrentUserId, claim.Comments.Max(c => c.CommentId));
      }


      if (isMyClaim || claim.HasMasterAccess(CurrentUserId, acl => acl.CanManageMoney))
      {
        //Finance admins can create any payment. User also can create any payment, but it will be moderated
        claimViewModel.PaymentTypes = claim.Project.ActivePaymentTypes;
      }
      else
      {
        //All other master can create only payment from user to himself.
        claimViewModel.PaymentTypes = claim.Project.ActivePaymentTypes.Where(pt => pt.UserId == CurrentUserId);
      }


      if (claim.Character !=null)
      {
        claimViewModel.ParentGroups = CharacterParentGroupsViewModel.FromCharacter(claim.Character, hasMasterAccess);

      }

      if (claim.IsApproved)
      {
        var plotElements = await _plotRepository.GetPlotsForCharacter(claim.Character);
        claimViewModel.Plot =
          claim.Character.GetOrderedPlots(plotElements).Select(
            p => PlotElementViewModel.FromPlotElement(p, hasMasterAccess));
      }
      else
      {
        claimViewModel.Plot = Enumerable.Empty<PlotElementViewModel>();
      }
      return View(claimViewModel);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    // ReSharper disable once UnusedParameter.Global
    public async Task<ActionResult> Edit(int projectId, int claimId, string ignoreMe)
    {
      var claim = await ProjectRepository.GetClaim(projectId, claimId);
      var error = WithClaim(claim);
      if (error != null)
      {
        return error;
      }
      if ((!claim.IsApproved && !claim.Project.HasMasterAccess(CurrentUserId)) || claim.CharacterId == null)
      {
        return await Edit(projectId, claimId);
      }
      try
      {
        await
          ProjectService.SaveCharacterFields(projectId, (int) claim.CharacterId, CurrentUserId, GetCharacterFieldValuesFromPost());
        return RedirectToAction("Edit", "Claim", new {projectId, claimId});
      }
      catch
      {
        return await Edit(projectId, claimId);
      }
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> ApproveByMaster(AddCommentViewModel viewModel)
    {
      var claim = await ProjectRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      var error = AsMaster(claim);
      if (error != null)
      {
        return error;
      }

      try
      {
        if (viewModel.HideFromUser)
        {
          throw new DbEntityValidationException();
        }
        await
          _claimService.AppoveByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText.Contents);

        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }
      catch
      {
        //TODO: Message that comment is not added
        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeclineByMaster(AddCommentViewModel viewModel)
    {
      var claim = await ProjectRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      var error = AsMaster(claim);
      if (error != null)
      {
        return error;
      }

      try
      {
        if (viewModel.HideFromUser)
        {
          throw new DbEntityValidationException();
        }
        await
          _claimService.DeclineByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText.Contents);

        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }
      catch
      {
        //TODO: Message that comment is not added
        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }

    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> RestoreByMaster(AddCommentViewModel viewModel)
    {
      var claim = await ProjectRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      var error = AsMaster(claim);
      if (error != null)
      {
        return error;
      }

      try
      {
        if (!ModelState.IsValid)
        {
          throw new DbEntityValidationException();
        }
        await
          _claimService.RestoreByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText.Contents);

        return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
      }
      catch
      {
        //TODO: Message that comment is not added
        return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
      }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeclineByPlayer(AddCommentViewModel viewModel)
    {
      var claim = await ProjectRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      var error = WithMyClaim(claim);
      if (error != null)
      {
        return error;
      }
      try
      {
        if (viewModel.HideFromUser)
        {
          throw new DbEntityValidationException();
        }
        await
          _claimService.DeclineByPlayer(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText.Contents);

        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }
      catch
      {
        //TODO: Message that comment is not added
        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ChangeResponsible(int projectId, int claimId, int responsibleMasterId)
    {
      var claim = await ProjectRepository.GetClaim(projectId, claimId);
      var error = AsMaster(claim);
      if (error != null)
      {
        return error;
      }
      try
      {
        await _claimService.SetResponsible(projectId, claimId, CurrentUserId, responsibleMasterId);
        return ReturnToClaim(claimId, projectId);
      }
      catch
      {
        //TODO: Message 
        return RedirectToAction("Edit", "Claim", new {claimId, projectId});
      }
      
    }

    [HttpGet, Authorize]
    public Task<ActionResult> ActiveList(int projectid, string export) 
      => MasterList(projectid, claim => claim.IsActive, "ActiveList", export);

    [HttpGet, Authorize]
    public Task<ActionResult> DeclinedList(int projectid, string export)
      => MasterList(projectid, claim => !claim.IsActive, "DeclinedList", export);

    /// <param name="viewModel"></param>
    /// <param name="claimTarget">Note that name is hardcoded in view. (TODO improve)</param>
    public async Task<ActionResult> Move(AddCommentViewModel viewModel, string claimTarget)
    {
      var claim = await ProjectRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      var error = AsMaster(claim);
      if (error != null)
      {
        return error;
      }

      try
      {
        if (!ModelState.IsValid)
        {
          throw new DbEntityValidationException();
        }
        var characterGroupId = claimTarget.UnprefixNumber(GroupFieldPrefix);
        var characterId = claimTarget.UnprefixNumber(CharFieldPrefix);
        await
          _claimService.MoveByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText.Contents, characterGroupId, characterId);

        return ReturnToClaim(viewModel);
      }
      catch
      {
        //TODO: Message that comment is not added
        return ReturnToClaim(viewModel);
      }
    }

    private ActionResult ReturnToClaim(AddCommentViewModel viewModel)
    {
      return ReturnToClaim(viewModel.ClaimId, viewModel.ProjectId);
    }

    private ActionResult ReturnToClaim(int claimId, int projectId)
    {
      return RedirectToAction("Edit", "Claim", new {claimId, projectId});
    }

    [Authorize, HttpGet]
    public async Task<ActionResult> MyClaim(int projectId)
    {
      var claims = (await _claimsRepository.GetMyClaimsForProject(CurrentUserId, projectId)).ToList();

      if (claims.Count == 0)
      {
        var project = await ProjectRepository.GetProjectAsync(projectId);
        return RedirectToAction("AddForGroup", new {projectId, project.RootGroup.CharacterGroupId});
      }

      if (claims.Count(c => c.IsApproved) == 1)
      {
        return ReturnToClaim(claims.Single(c => c.IsApproved).ClaimId, projectId);
      }

      if (claims.Count(c => c.IsInDiscussion) == 1)
      {
        return ReturnToClaim(claims.Single(c => c.IsInDiscussion).ClaimId, projectId);
      }

      if (claims.Count == 1)
      {
        return ReturnToClaim(claims.Single().ClaimId, projectId);
      }

      return RedirectToAction("My", "Claim");

    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> FinanceOperation(FinOperationViewModel viewModel)
    {
      var claim = await ProjectRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      var error = WithClaim(claim);
      if (error != null)
      {
        return error;
      }
      try
      {
        if (!ModelState.IsValid)
        {
          return await Edit(viewModel.ProjectId, viewModel.ClaimId);
        }


        await
          FinanceService.FeeAcceptedOperation(claim.ProjectId, claim.ClaimId, CurrentUserId,
            viewModel.CommentText.Contents, viewModel.OperationDate, viewModel.FeeChange, viewModel.Money,
            viewModel.PaymentTypeId);
        
        return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
      }
      catch
      {
        return await Edit(viewModel.ProjectId, viewModel.ClaimId);
      }
    }
  }
}