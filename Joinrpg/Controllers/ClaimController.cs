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
using JoinRpg.Data.Write.Interfaces;
using System.Collections.Generic;

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
          CurrentUserId, viewModel.ClaimText, 
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

      var printPlugins = claim.HasMasterAccess(CurrentUserId) && claim.IsApproved
        ? (await PluginFactory.GetPossibleOperations<IPrintCardPluginOperation>(claim.ProjectId)).Where(
          p => p.AllowPlayerAccess || claim.HasMasterAccess(CurrentUserId))
        : Enumerable.Empty<PluginOperationData<IPrintCardPluginOperation>>();

      var plots = claim.IsApproved && claim.Character != null
        ? await _plotRepository.GetPlotsForCharacter(claim.Character)
        : new PlotElement[] {};
      var claimViewModel = new ClaimViewModel(CurrentUserId, claim, printPlugins, plots);

      if (claimViewModel.Comments.Any(c => !c.IsRead))
      {
        await
          _claimService.UpdateReadCommentWatermark(claim.ProjectId, claim.ClaimId, CurrentUserId,
            claim.Comments.Max(c => c.CommentId));
      }

            var user = await GetCurrentUserAsync();
            List<int> parentGroups= _claimService.GetGroupHierarchy(claim.CharacterGroupId??claim.Character.Groups.FirstOrDefault().CharacterGroupId);
            bool isHierarchSubscribe;
            var subscriptions = user.Subscriptions.Where(s=> {
                isHierarchSubscribe = false;
                //parentGroups = new List<int>();
                //if ((s.CharacterId!=null&&(s.CharacterId == claim.CharacterId||claim.Character!=null&&(s.CharacterId==claim.Character.CharacterId)))||(s.CharacterGroupId!=null&&(s.CharacterGroupId==claim.CharacterGroupId||claim.Character!=null&&s.CharacterGroupId==claim.Character.Groups.FirstOrDefault().CharacterGroupId||claim.Group!=null&&s.CharacterGroupId==claim.Group.CharacterGroupId))) {
               /*     if (s.CharacterGroupId != null)
                    {
                        parentGroups = _claimService.GetGroupHierarchy(s.CharacterGroupId);
                    }
                    if (s.ClaimId != null && s.Claim.CharacterGroupId != null)
                    {
                        parentGroups = _claimService.GetGroupHierarchy(s.Claim.CharacterGroupId);
                    }
                    if (s.ClaimId != null && s.Claim.CharacterId != null)
                    {
                        parentGroups = _claimService.GetGroupHierarchy(s.Claim.Character.Groups.First().CharacterGroupId);
                    }
                    if (s.CharacterId != null )
                    {
                        parentGroups = _claimService.GetGroupHierarchy(s.CharacterGroupId);
                    }*/
               // }
                foreach(var el in parentGroups)
                {
                    if (s.CharacterGroupId == el)
                    {
                        isHierarchSubscribe = true;
                    }
                }
                return s.ProjectId == claim.ProjectId && (s.CharacterGroupId == (claim.CharacterGroupId ?? -1) || s.ClaimId == claim.ClaimId || s.CharacterId == (claim.CharacterId ?? -1)|| isHierarchSubscribe);
            });
            claimViewModel.Subscriptions = subscriptions;
            claimViewModel.SubscriptionTooltip = _claimService.GetSubscriptionTooltip(subscriptions);
            //if (claimViewModel.CharacterGroupId == null) { claimViewModel.CharacterGroupId = claim.CharacterGroupId ?? claim.Character.Groups.FirstOrDefault().CharacterGroupId; }
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
          _claimService.AppoveByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText);

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
          _claimService.OnHoldByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText);

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
          _claimService.DeclineByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText);

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
          _claimService.RestoreByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText);

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
          _claimService.DeclineByPlayer(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText);

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
          _claimService.MoveByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText, characterGroupId, characterId);

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
            viewModel.CommentText, viewModel.OperationDate, viewModel.FeeChange, viewModel.Money,
            viewModel.PaymentTypeId);
        
        return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
      }
      catch
      {
        return await Edit(viewModel.ProjectId, viewModel.ClaimId);
      }
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> ChangeFee(int claimid, int projectid, int feeValue)
    {
      var claim = await _claimsRepository.GetClaim(projectid, claimid);
      var error = WithClaim(claim);
      if (error != null)
      {
        return error;
      }
      try
      {
        if (!ModelState.IsValid)
        {
          return await Edit(projectid, claimid);
        }

        await
          FinanceService.ChangeFee(claim.ProjectId, claim.ClaimId, feeValue, CurrentUserId);

        return RedirectToAction("Edit", "Claim", new { claimid, projectid });
      }
      catch
      {
        return await Edit(projectid, claimid);
      }
    }

        public async Task<String> Subscribe(int projectid,int claimid)
        {

            var user = await GetCurrentUserAsync();
            var claim = await _claimsRepository.GetClaim(projectid, claimid);

            var error = AsMaster(claim);
            if (error != null)
            {
                return error.ToString();
            }

            await _claimService.SubscribeClaimToUser(projectid, claimid, user.UserId, claim.CharacterId, claim.CharacterGroupId);

            return User.Identity.Name+"; "+claim.Name;
        }
        public async Task<String> Unsubscribe(int projectid, int claimid, int subscribeid)
        {

            var user = await GetCurrentUserAsync();
            var claim = await _claimsRepository.GetClaim(projectid, claimid);

            var error = AsMaster(claim);
            if (error != null)
            {
                return error.ToString();
            }

            await _claimService.UnsubscribeClaimToUser(projectid, claimid, user.UserId, subscribeid);

            return User.Identity.Name + "; " + claim.Name+ "; subscribeid " + subscribeid+" - удалено";
        }
    }
}