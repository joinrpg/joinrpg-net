using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.Models.Print;

namespace JoinRpg.Web.Controllers
{
  public class ClaimController : ControllerGameBase
  {
    private readonly IClaimService _claimService;
    private readonly IPlotRepository _plotRepository;
    private readonly IClaimsRepository _claimsRepository;
    private IFinanceService FinanceService { get; }
    private IPluginFactory PluginFactory { get; }

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
      var field = await ProjectRepository.GetGroupAsync(projectid, characterGroupId);
      return WithEntity(field.Project) ?? View("Add", AddClaimViewModel.Create(field, GetCurrentUser()));
    }

    public ClaimController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IClaimService claimService, IPlotRepository plotRepository,
      IClaimsRepository claimsRepository, IFinanceService financeService, IExportDataService exportDataService, IPluginFactory pluginFactory)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      _claimService = claimService;
      _plotRepository = plotRepository;
      _claimsRepository = claimsRepository;
      FinanceService = financeService;
      PluginFactory = pluginFactory;
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
          CurrentUserId, viewModel.ClaimText?.Contents, 
          GetCustomFieldValuesFromPost());

        return RedirectToAction(
          "SetupProfile",
          "Manage",
          new { checkContactsMessage = true});
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        var source = await GetClaimSource(viewModel.ProjectId, viewModel.CharacterGroupId, viewModel.CharacterId);
        //TODO: Отображать ошибки верно
        return View(viewModel.Fill(source, GetCurrentUser()));
      }
    }

    private async Task<IClaimSource> GetClaimSource(int projectId, int? characterGroupId, int? characterId)
    {
      if (characterGroupId != null)
      {
        return await ProjectRepository.GetGroupAsync(projectId, (int) characterGroupId);
      }
      if (characterId != null)
      {
        return await ProjectRepository.GetCharacterAsync(projectId, (int) characterId);
      }
      throw new InvalidOperationException();
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Edit(int projectId, int claimId)
    {
      var claim = await _claimsRepository.GetClaimWithDetails(projectId, claimId);
      return await ShowClaim(claim);
    }

    private async Task<ActionResult> ShowClaim(Claim claim)
    {
      var error = WithClaim(claim);
      if (error != null)
      {
        return error;
      }

      var claimViewModel = new ClaimViewModel()
      {
        ClaimId = claim.ClaimId,
        Comments =
          claim.Comments.Where(comment => comment.ParentCommentId == null)
            .Select(comment => new CommentViewModel(comment, CurrentUserId)).OrderBy(c => c.CreatedTime),
        HasMasterAccess = claim.HasMasterAccess(CurrentUserId),
        CanManageThisClaim = claim.CanManageClaim(CurrentUserId),
        IsMyClaim = claim.PlayerUserId == CurrentUserId,
        Player = claim.Player,
        ProjectId = claim.ProjectId,
        Status = claim.ClaimStatus,
        CharacterGroupId = claim.CharacterGroupId,
        GroupName = claim.Group?.CharacterGroupName,
        CharacterId = claim.CharacterId,
        CharacterActive = claim.Character?.IsActive,
        OtherClaimsForThisCharacterCount = claim.IsApproved ? 0 : claim.OtherClaimsForThisCharacter().Count(),
        HasOtherApprovedClaim = !claim.IsApproved && claim.OtherClaimsForThisCharacter().Any(c => c.IsApproved),
        Data = new CharacterTreeBuilder(claim.Project.RootGroup, CurrentUserId).Generate(),
        OtherClaimsFromThisPlayerCount = claim.IsApproved ? 0 : claim.OtherPendingClaimsForThisPlayer().Count(),
        Description = new MarkdownViewModel(claim.Character?.Description),
        Masters =
          claim.Project.GetMasterListViewModel()
            .Union(new MasterListItemViewModel() {Id = "-1", Name = "Нет"}),
        ResponsibleMasterId = claim.ResponsibleMasterUserId ?? -1,
        ResponsibleMaster = claim.ResponsibleMasterUser,
        Fields = new CustomFieldsViewModel(CurrentUserId, claim),
        Navigation = CharacterNavigationViewModel.FromClaim(claim, CurrentUserId, CharacterNavigationPage.Claim),
        ClaimFee = new ClaimFeeViewModel()
        {
          CurrentTotalFee = claim.ClaimTotalFee(),
          CurrentBalance = claim.ClaimBalance(),
          CurrentFee = claim.ClaimCurrentFee()
        },
        Problems = claim.GetProblems().Select(p => new ProblemViewModel(p)).ToList(),
        PlayerDetails = UserProfileDetailsViewModel.FromUser(claim.Player),
        PrintPlugins = claim.HasMasterAccess(CurrentUserId) && claim.IsApproved
          ? (await PluginFactory.GetPossibleOperations<IPrintCardPluginOperation>(claim.ProjectId)).Where(
            p => p.AllowPlayerAccess || claim.HasMasterAccess(CurrentUserId)).Select(
              PluginOperationDescriptionViewModel.Create)
          : Enumerable.Empty<PluginOperationDescriptionViewModel>()
      };

      if (claimViewModel.Comments.Any(c => !c.IsRead))
      {
        await
          _claimService.UpdateReadCommentWatermark(claim.ProjectId, claim.ClaimId, CurrentUserId,
            claim.Comments.Max(c => c.CommentId));
      }


      if (claim.PlayerUserId == CurrentUserId || claim.HasMasterAccess(CurrentUserId, acl => acl.CanManageMoney))
      {
        //Finance admins can create any payment. User also can create any payment, but it will be moderated
        claimViewModel.PaymentTypes = claim.Project.ActivePaymentTypes;
      }
      else
      {
        //All other master can create only payment from user to himself.
        claimViewModel.PaymentTypes = claim.Project.ActivePaymentTypes.Where(pt => pt.UserId == CurrentUserId);
      }


      if (claim.Character != null)
      {
        claimViewModel.ParentGroups = new CharacterParentGroupsViewModel(claim.Character, claim.HasMasterAccess(CurrentUserId));
      }

      if (claim.IsApproved)
      {
        var plotElements = await _plotRepository.GetPlotsForCharacter(claim.Character);
        
        claimViewModel.Plot =
          // ReSharper disable once PossibleNullReferenceException
          claim.Character.GetOrderedPlots(plotElements).ToViewModels(claim.HasMasterAccess(CurrentUserId), claim.Character.CharacterId);
      }
      else
      {
        claimViewModel.Plot = Enumerable.Empty<PlotElementViewModel>();
      }
      return View("Edit", claimViewModel);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(int projectId, int claimId, [UsedImplicitly] string ignoreMe)
    {
      var claim = await _claimsRepository.GetClaim(projectId, claimId);
      var error = WithClaim(claim);
      if (error != null)
      {
        return error;
      }
      try
      {
        await
          _claimService.SaveFieldsFromClaim(projectId, claimId, CurrentUserId, GetCustomFieldValuesFromPost());
        return RedirectToAction("Edit", "Claim", new {projectId, claimId});
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return await Edit(projectId, claimId);
      }
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> ApproveByMaster(AddCommentViewModel viewModel)
    {
      var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      var error = AsMaster(claim);
      if (error != null)
      {
        return error;
      }

      try
      {
        await
          _claimService.AppoveByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText.Contents);

        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return await ShowClaim(claim);
      }
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> OnHoldByMaster(AddCommentViewModel viewModel)
    {
      var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      var error = AsMaster(claim);
      if (error != null)
      {
        return error;
      }

      try
      {
        await
          _claimService.OnHoldByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText.Contents);

        return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return await ShowClaim(claim);
      }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeclineByMaster(AddCommentViewModel viewModel)
    {
      var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
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
      var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
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
      var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
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
      var claim = await _claimsRepository.GetClaim(projectId, claimId);
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


    /// <param name="viewModel"></param>
    /// <param name="claimTarget">Note that name is hardcoded in view. (TODO improve)</param>
    public async Task<ActionResult> Move(AddCommentViewModel viewModel, string claimTarget)
    {
      var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      var error = AsMaster(claim);
      if (error != null)
      {
        return error;
      }

      try
      {
        if (!ModelState.IsValid)
        {
          return await ShowClaim(claim);
        }
        var characterGroupId = claimTarget.UnprefixNumber(CharacterAndGroupPrefixer.GroupFieldPrefix);
        var characterId = claimTarget.UnprefixNumber(CharacterAndGroupPrefixer.CharFieldPrefix);
        await
          _claimService.MoveByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText.Contents, characterGroupId, characterId);

        return ReturnToClaim(viewModel);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return await ShowClaim(claim);
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

      var claimId = claims.TrySelectSingleClaim()?.ClaimId;

      return claimId != null ? ReturnToClaim((int) claimId, projectId) : RedirectToAction("My", "ClaimList");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> FinanceOperation(FeeAcceptanceViewModel viewModel)
    {
      var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
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