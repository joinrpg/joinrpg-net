using System;
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
      IClaimsRepository claimsRepository, IFinanceService financeService) : base(userManager, projectRepository, projectService)
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
    public Task<ActionResult> ForPlayer(int projectId, int userId)
      => MasterList(projectId, cl => cl.IsActive & cl.PlayerUserId == userId, "ForPlayer");

    [HttpGet, Authorize]
    public Task<ActionResult> ListForGroupDirect(int projectId, int characterGroupId)
    {
      ViewBag.CharacterGroupId = characterGroupId;
      ViewBag.ProjectId = projectId;
      return MasterList(projectId, cl => cl.CharacterGroupId == characterGroupId, "ListForGroupDirect");
    }

    public Task<ActionResult> ListForGroup(int projectId, int characterGroupId)
    {
      ViewBag.CharacterGroupId = characterGroupId;
      ViewBag.ProjectId = projectId;
      return MasterList(projectId, cl => cl.PartOfGroup(characterGroupId), "ListForGroup");
    }

    private async Task<ActionResult> MasterList(int projectId, Func<Claim, bool> predicate, [AspMvcView] string viewName)
    {
      var project = await _claimsRepository.GetClaims(projectId);
      return AsMaster(project) ??
             View(viewName, project.Claims.Where(predicate).Select(ClaimListItemViewModel.FromClaim));
    }

    [HttpGet, Authorize]
    public Task<ActionResult> Discussing(int projectid)
      => MasterList(projectid, claim => claim.IsInDiscussion, "Discussing");

    [HttpGet, Authorize]
    public Task<ActionResult> Responsible(int projectid, int responsibleMasterId)
      =>
        MasterList(projectid, claim => claim.ResponsibleMasterUserId == responsibleMasterId && claim.IsActive,
          "Responsible");

    [HttpGet, Authorize]
    public async Task<ActionResult> Problems(int projectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project) ??
             View(
               (await _claimService.GetProblemClaims(projectId)).Select(ClaimProblemListItemViewModel.FromClaimProblem));
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
        ClaimName = claim.Name,
        Comments = claim.Comments.Where(comment => comment.ParentCommentId == null),
        HasMasterAccess = hasMasterAccess,
        HasPlayerAccessToCharacter = hasMasterAccess || hasPlayerAccess,
        HasApproveRejectClaim = claim.HasMasterAccess(CurrentUserId, acl => acl.CanApproveClaims),
        CanAcceptCash = claim.HasMasterAccess(CurrentUserId, acl => acl.CanAcceptCash),
        CanManageMoney = claim.HasMasterAccess(CurrentUserId, acl => acl.CanManageMoney),
        IsMyClaim = isMyClaim,
        Player = claim.Player,
        ProjectId = claim.ProjectId,
        ProjectName = claim.Project.ProjectName,
        Status = claim.ClaimStatus,
        CharacterGroupId = claim.CharacterGroupId,
        GroupName = claim.Group?.CharacterGroupName,
        CharacterId = claim.CharacterId,
        CharacterName = claim.Character?.CharacterName,
        OtherClaimsForThisCharacterCount = claim.IsApproved ? 0 : claim.OtherClaimsForThisCharacter().Count(),
        HasOtherApprovedClaim = !claim.IsApproved && claim.OtherClaimsForThisCharacter().Any(c => c.IsApproved),
        Data = CharacterGroupListViewModel.FromGroupAsMaster(claim.Project.RootGroup),
        OtherClaimsFromThisPlayerCount = claim.IsApproved ? 0 : claim.OtherClaimsForThisPlayer().Count(),
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
        }
        
      };

      if (claim.Character !=null)
      {
        claimViewModel.ParentGroups = CharacterParentGroupsViewModel.FromCharacter(claim.Character, hasMasterAccess);

      }

      if (claim.IsApproved)
      {
        claimViewModel.Plot =
          (await _plotRepository.GetPlotsForCharacter(claim.Character)).Select(
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
    public Task<ActionResult> ActiveList(int projectid) => MasterList(projectid, claim => claim.IsActive, "ActiveList");

    public Task<ActionResult> DeclinedList(int projectid) => MasterList(projectid, claim => !claim.IsActive, "DeclinedList");

    /// <summary>
    /// 
    /// </summary>
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

    public async Task<ActionResult> FinanceOperation(FinanceOperationViewModel viewModel)
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
          return await Edit(viewModel.ProjectId, viewModel.ClaimId);
        }


        await
          FinanceService.FeeAcceptedOperation(claim.ProjectId, claim.ClaimId, CurrentUserId,
            viewModel.CommentText.Contents, viewModel.OperationDate, viewModel.FeeChange, viewModel.Money,
            claim.Project.PaymentTypes.Single(pt => pt.IsCash).PaymentTypeId);
        
        return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
      }
      catch
      {
        return await Edit(viewModel.ProjectId, viewModel.ClaimId);
      }
    }
  }
}