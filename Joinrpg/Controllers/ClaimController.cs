using System;
using System.Collections.Generic;
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
using JoinRpg.Web.Filter;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Accommodation;

namespace JoinRpg.Web.Controllers
{
  public class ClaimController : ControllerGameBase
  {
    private readonly IClaimService _claimService;
    private readonly IPlotRepository _plotRepository;
    private readonly IClaimsRepository _claimsRepository;
    private readonly IAccommodationRequestRepository _accommodationRequestRepository;
    private readonly IAccommodationRepository _accommodationRepository;
    private IAccommodationService AccommodationService { get; }
    private IAccommodationInviteService AccommodationInviteService { get; }
    private IFinanceService FinanceService { get; }
    private IPluginFactory PluginFactory { get; }
    private ICharacterRepository CharacterRepository { get; }
    private IAccommodationInviteRepository AccommodationInviteRepository { get; }
    private IUriService UriService { get; }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> AddForCharacter(int projectid, int characterid)
    {
      var field = await CharacterRepository.GetCharacterAsync(projectid, characterid);
            if (field == null) return HttpNotFound();
        return View("Add", AddClaimViewModel.Create(field, CurrentUserId));
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> AddForGroup(int projectid, int? characterGroupId)
    {
        if (characterGroupId == null)
        {
            var project = await ProjectRepository.GetProjectAsync(projectid);
            return RedirectToAction("AddForGroup",
                new {project.ProjectId, project.RootGroup.CharacterGroupId});
        }
      var field = await ProjectRepository.GetGroupAsync(projectid, characterGroupId.Value);
      if (field == null) return HttpNotFound();
        return View("Add", AddClaimViewModel.Create(field, CurrentUserId));
    }

      public ClaimController(ApplicationUserManager userManager,
          IProjectRepository projectRepository,
          IProjectService projectService,
          IClaimService claimService,
          IPlotRepository plotRepository,
          IClaimsRepository claimsRepository,
          IFinanceService financeService,
          IExportDataService exportDataService,
          IPluginFactory pluginFactory,
          ICharacterRepository characterRepository,
          IUriService uriService,
          IAccommodationRequestRepository accommodationRequestRepository,
          IAccommodationRepository accommodationRepository,
          IAccommodationService accommodationService,
          IAccommodationInviteService accommodationInviteService,
          IAccommodationInviteRepository accommodationInviteRepository)
          : base(userManager, projectRepository, projectService, exportDataService)
      {
          _claimService = claimService;
          _plotRepository = plotRepository;
          _claimsRepository = claimsRepository;
          _accommodationRequestRepository = accommodationRequestRepository;
          _accommodationRepository = accommodationRepository;
          AccommodationService = accommodationService;
          AccommodationInviteService = accommodationInviteService;
          AccommodationInviteRepository = accommodationInviteRepository;
          FinanceService = financeService;
          PluginFactory = pluginFactory;
          CharacterRepository = characterRepository;
          UriService = uriService;
      }

      [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Add(AddClaimViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      if (project == null)
      {
        return HttpNotFound();
      }

      try
      {
        await _claimService.AddClaimFromUser(viewModel.ProjectId, viewModel.CharacterGroupId, viewModel.CharacterId, viewModel.ClaimText, 
          GetCustomFieldValuesFromPost());

        return RedirectToAction(
          "SetupProfile",
          "Manage",
          new { checkContactsMessage = true});
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
         var source = await ProjectRepository.GetClaimSource(viewModel.ProjectId, viewModel.CharacterGroupId, viewModel.CharacterId).ConfigureAwait(false);
          return View(viewModel.Fill(source,CurrentUserId));
      }
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Edit(int projectId, int claimId)
    {
      var claim = await _claimsRepository.GetClaimWithDetails(projectId, claimId).ConfigureAwait(false);
      return await ShowClaim(claim).ConfigureAwait(false);
    }

    private async Task<ActionResult> ShowClaim(Claim claim)
    {
      var error = WithClaim(claim);
      if (error != null)
      {
        return error;
      }

      var printPlugins = claim.HasMasterAccess(CurrentUserId) && claim.IsApproved
        ? (PluginFactory.GetProjectOperations<IPrintCardPluginOperation>(claim.Project)).Where(
          p => p.AllowPlayerAccess || claim.HasMasterAccess(CurrentUserId))
        : Enumerable.Empty<PluginOperationData<IPrintCardPluginOperation>>();

      var currentUser = await GetCurrentUserAsync().ConfigureAwait(false);

      var plots = claim.IsApproved && claim.Character != null
        ? await _plotRepository.GetPlotsForCharacter(claim.Character).ConfigureAwait(false)
        : new PlotElement[] { };

      IEnumerable<ProjectAccommodationType> availableAccommodation = null;
      IEnumerable<AccommodationRequest> requestForAccommodation = null;
      IEnumerable<AccommodationPotentialNeighbors> potentialNeighbors = null;
      IEnumerable<AccommodationInvite> incomingInvite = null;
      IEnumerable<AccommodationInvite> outgoingInvite = null;


      if (claim.Project.Details.EnableAccommodation)
      { 
        availableAccommodation = await
            _accommodationRepository.GetAccommodationForProject(claim.ProjectId).ConfigureAwait(false);
        requestForAccommodation = await _accommodationRequestRepository
            .GetAccommodationRequestForClaim(claim.ClaimId).ConfigureAwait(false);
        var acceptedRequest =  requestForAccommodation
            .FirstOrDefault(request => request.IsAccepted == AccommodationRequest.InviteState.Accepted);
            var acceptedRequestId = acceptedRequest?.Id;
          var acceptedRequestAccommodationTypeIdId = acceptedRequest?.AccommodationTypeId;

        if (acceptedRequestId != null)
        {
          var sameRequest = (await
              _accommodationRequestRepository.GetClaimsWithSameAccommodationTypeToInvite(
                  acceptedRequestAccommodationTypeIdId.Value).ConfigureAwait(false)).Where(c=>c.ClaimId!=claim.ClaimId)
              .Select(c => new AccommodationPotentialNeighbors(c, NeighborType.WithSameType)); ;
          var noRequest = (await
              _accommodationRequestRepository.GetClaimsWithOutAccommodationRequest(claim.ProjectId).ConfigureAwait(false)).Select(c => new AccommodationPotentialNeighbors(c, NeighborType.NoRequest)); ;
          var currentNeighbors = (await
             _accommodationRequestRepository.GetClaimsWithSameAccommodationRequest(
                  acceptedRequestId.Value).ConfigureAwait(false)).Select(c => new AccommodationPotentialNeighbors(c, NeighborType.Current));
            potentialNeighbors = sameRequest.Union(noRequest).Where(element=> currentNeighbors.All(el => el.ClaimId != element.ClaimId));
        }

         incomingInvite = await AccommodationInviteRepository.GetIncomingInviteForClaim(claim).ConfigureAwait(false);
         outgoingInvite = await AccommodationInviteRepository.GetOutgoingInviteForClaim(claim).ConfigureAwait(false);
       }

      var claimViewModel = new ClaimViewModel(currentUser, claim, printPlugins, plots, UriService, availableAccommodation, requestForAccommodation, potentialNeighbors,incomingInvite,outgoingInvite);

      if (claim.CommentDiscussion.Comments.Any(c => !c.IsReadByUser(CurrentUserId)))
      {
        await
          _claimService.UpdateReadCommentWatermark(claim.ProjectId, claim.CommentDiscussion.CommentDiscussionId,
            claim.CommentDiscussion.Comments.Max(c => c.CommentId)).ConfigureAwait(false);
      }

      
      var parents = claim.GetTarget().GetParentGroupsToTop();
      claimViewModel.SubscriptionTooltip =
        claimViewModel.GetFullSubscriptionTooltip(parents, currentUser.Subscriptions, claimViewModel.ClaimId);

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
          _claimService.SaveFieldsFromClaim(projectId, claimId, GetCustomFieldValuesFromPost());
        return RedirectToAction("Edit", "Claim", new {projectId, claimId});
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return await Edit(projectId, claimId);
      }
    }

    [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
    public async Task<ActionResult> ApproveByMaster(ClaimOperationViewModel viewModel)
    {
      var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      if (claim == null)
      {
        return HttpNotFound();
      }

      try
      {
        await
          _claimService.ApproveByMaster(claim.ProjectId, claim.ClaimId, viewModel.CommentText);

        return ReturnToClaim(viewModel);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return await ShowClaim(claim);
      }
    }

    [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
    public async Task<ActionResult> OnHoldByMaster(ClaimOperationViewModel viewModel)
    {
      var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      if (claim == null)
      {
        return HttpNotFound();
      }

      try
      {
        await
          _claimService.OnHoldByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText);

        return ReturnToClaim(viewModel);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return await ShowClaim(claim);
      }
    }

    [HttpPost]
    [MasterAuthorize()]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeclineByMaster(ClaimOperationViewModel viewModel)
    {
      var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      if (claim == null)
      {
        return HttpNotFound();
      }

      try
      {
        if (!ModelState.IsValid)
        {
          return await ShowClaim(claim);
        }
        await
          _claimService.DeclineByMaster(claim.ProjectId, claim.ClaimId, (Claim.DenialStatus)viewModel.DenialStatus, viewModel.CommentText);

        return ReturnToClaim(viewModel);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return await ShowClaim(claim);
      }

    }

    [HttpPost]
    [MasterAuthorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> RestoreByMaster(ClaimOperationViewModel viewModel)
    {
      var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      if (claim == null)
      {
        return HttpNotFound();
      }

      try
      {
        if (!ModelState.IsValid)
        {
          return await ShowClaim(claim);
        }
        await
          _claimService.RestoreByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText);

        return ReturnToClaim(viewModel);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return await ShowClaim(claim);
      }
    }

    [HttpPost]
    [Authorize()]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeclineByPlayer(ClaimOperationViewModel viewModel)
    {
      var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      if (claim == null)
      {
        return HttpNotFound();
      }
      if (claim.PlayerUserId != CurrentUserId) return NoAccesToProjectView(claim.Project);
      try
      {
        if (!ModelState.IsValid)
        {
          return await ShowClaim(claim);
        }
        await
          _claimService.DeclineByPlayer(claim.ProjectId, claim.ClaimId, viewModel.CommentText);

        return ReturnToClaim(viewModel);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return await ShowClaim(claim);
      }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MasterAuthorize()]
    public async Task<ActionResult> ChangeResponsible(int projectId, int claimId, int responsibleMasterId)
    {
      var claim = await _claimsRepository.GetClaim(projectId, claimId);
      if (claim == null)
      {
        return HttpNotFound();
      }
      try
      {
        await _claimService.SetResponsible(projectId, claimId, CurrentUserId, responsibleMasterId);
        return ReturnToClaim(claimId, projectId);
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return await ShowClaim(claim);
      }

    }

    /// <param name="viewModel"></param>
    /// <param name="claimTarget">Note that name is hardcoded in view. (TODO improve)</param>
    [MasterAuthorize()]
    public async Task<ActionResult> Move(ClaimOperationViewModel viewModel, string claimTarget)
    {
      var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      if (claim == null)
      {
        return HttpNotFound();
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

    [MustUseReturnValue]
    private ActionResult ReturnToClaim(ClaimOperationViewModel viewModel)
    {
      return ReturnToClaim(viewModel.ClaimId, viewModel.ProjectId);
    }

    [MustUseReturnValue]
    private ActionResult ReturnToClaim(int claimId, int projectId)
    {
      return RedirectToAction("Edit", "Claim", new {claimId, projectId});
    }

    [Authorize, HttpGet]
    public async Task<ActionResult> MyClaim(int projectId)
    {
      var claims = await _claimsRepository.GetClaimsForPlayer(projectId, ClaimStatusSpec.Any, CurrentUserId);

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
      if (claim == null)
      {
        return HttpNotFound();
      }
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
              FinanceService.FeeAcceptedOperation(new FeeAcceptedOperationRequest()
              {
                  ProjectId = claim.ProjectId,
                  ClaimId = claim.ClaimId,
                  Contents = viewModel.CommentText,
                  FeeChange = viewModel.FeeChange,
                  Money = viewModel.Money,
                  OperationDate = viewModel.OperationDate,
                  PaymentTypeId = viewModel.PaymentTypeId,
              });
        
        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId });
      }
      catch
      {
        return await Edit(viewModel.ProjectId, viewModel.ClaimId);
      }
    }

    [MasterAuthorize(Permission.CanManageMoney), HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> ChangeFee(int claimid, int projectid, int feeValue)
    {
      try
      {
        if (!ModelState.IsValid)
        {
          return await Edit(projectid, claimid);
        }

        await
          FinanceService.ChangeFee(projectid, claimid, feeValue);

        return RedirectToAction("Edit", "Claim", new { claimid, projectid });
      }
      catch
      {
        return await Edit(projectid, claimid);
      }
    }

    [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
    public async Task<ActionResult> Subscribe(int projectid, int claimid)
    {

      var user = await GetCurrentUserAsync();
      var claim = await _claimsRepository.GetClaim(projectid, claimid);
      if (claim == null)
      {
        return HttpNotFound();
      }

      var claimViewModel = new ClaimViewModel(user, claim,
        Enumerable.Empty<PluginOperationData<IPrintCardPluginOperation>>(), new PlotElement[] { },
          UriService);

      await _claimService.SubscribeClaimToUser(projectid, claimid);
      var parents = claim.GetTarget().GetParentGroupsToTop();

      var tooltip = claimViewModel.GetFullSubscriptionTooltip(parents, user.Subscriptions, claimViewModel.ClaimId);

      return Json(tooltip, JsonRequestBehavior.AllowGet);
    }

    [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
    public async Task<ActionResult> Unsubscribe(int projectid, int claimid)
    {

      var user = await GetCurrentUserAsync();
      var claim = await _claimsRepository.GetClaim(projectid, claimid);

      if (claim == null)
      {
        return HttpNotFound();
      }

      var claimViewModel = new ClaimViewModel(user, claim,
        Enumerable.Empty<PluginOperationData<IPrintCardPluginOperation>>(), new PlotElement[] { },
          UriService);


      await _claimService.UnsubscribeClaimToUser(projectid, claimid);
      var parents = claim.GetTarget().GetParentGroupsToTop();

      var tooltip = claimViewModel.GetFullSubscriptionTooltip(parents, user.Subscriptions, claimViewModel.ClaimId);

      return Json(tooltip, JsonRequestBehavior.AllowGet);
    }

    private ActionResult WithClaim(Claim claim)
    {
      if (claim == null)
      {
        return HttpNotFound();
      }
      if (!claim.HasAccess(CurrentUserId, ExtraAccessReason.Player))
      {
        return NoAccesToProjectView(claim.Project);
      }

      return null;
    }

      [MasterAuthorize(Permission.CanManageMoney), ValidateAntiForgeryToken]
      public async Task<ActionResult> MarkPreferential(int claimid,
          int projectid,
          bool preferential)
      {
          try
          {
              if (!ModelState.IsValid)
              {
                  return await Edit(projectid, claimid);
              }

              await
                  FinanceService.MarkPreferential(new MarkPreferentialRequest
                  {
                      ProjectId = projectid,
                      ClaimId = claimid,
                      Preferential = preferential,
                  });

              return RedirectToAction("Edit", "Claim", new {claimid, projectid});
          }
          catch
          {
              return await Edit(projectid, claimid);
          }
      }

        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RequestPreferentialFee(
            MarkMeAsPreferentialViewModel viewModel)
        {
            var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
            if (claim == null)
            {
                return HttpNotFound();
            }
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
                    FinanceService.RequestPreferentialFee(new MarkMeAsPreferentialFeeOperationRequest()
                    {
                        ProjectId = claim.ProjectId,
                        ClaimId = claim.ClaimId,
                        Contents = viewModel.CommentText,
                        OperationDate = viewModel.OperationDate,
                    })
                        .ConfigureAwait(false);

                return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId });
            }
            catch
            {
                return await Edit(viewModel.ProjectId, viewModel.ClaimId);
            }
            
        }

       [ValidateAntiForgeryToken]
       [HttpPost]
       public async Task<ActionResult> PostAccommodationRequest(
          AccommodationRequestViewModel viewModel)
       {
         var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId).ConfigureAwait(false);
            if (claim == null)
         {
           return HttpNotFound();
         }
         var error = WithClaim(claim);
         if (error != null)
         {
           return error;
         }
         try
         {
           if (!ModelState.IsValid)
           {
              return await Edit(viewModel.ProjectId, viewModel.ClaimId).ConfigureAwait(false);
           }


           await _claimService.SetAccommodationType(viewModel.ProjectId,
             viewModel.ClaimId,
             viewModel.AccommodationTypeId).ConfigureAwait(false);

           return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
         }
         catch
         {
           return await Edit(viewModel.ProjectId, viewModel.ClaimId).ConfigureAwait(false);
         }
       }

      [ValidateAntiForgeryToken]
      [HttpPost]
      public async Task<ActionResult> Invite(InviteRequestViewModel viewModel)
      {
          var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId).ConfigureAwait(false);
          if (project == null)
          {
              return HttpNotFound();
          }

          if (!ModelState.IsValid)
          {
              return await Edit(viewModel.ProjectId, viewModel.ClaimId).ConfigureAwait(false);
          }

          await AccommodationInviteService.CreateAccommodationInviteToGroupOrClaim(viewModel.ProjectId,
              viewModel.ClaimId,
              viewModel.ReceiverClaimOrAccommodationRequest,
              viewModel.RequestId,
             InviteRequestViewModel.AccommodationRequestPrefix).ConfigureAwait(false);

          return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
      }

      [ValidateAntiForgeryToken]
      [HttpPost]
        public async Task<ActionResult> DeclineInvite(InviteRequestViewModel viewModel)
      {
          var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId).ConfigureAwait(false);
          if (project == null)
          {
              return HttpNotFound();
          }

          if (!ModelState.IsValid)
          {
              return await Edit(viewModel.ProjectId, viewModel.ClaimId).ConfigureAwait(false);
          }

          await AccommodationInviteService.CancelOrDeclineAccommodationInvite(viewModel.InviteId,viewModel.InviteState).ConfigureAwait(false);

          return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
      }

      [ValidateAntiForgeryToken]
      [HttpPost]
      public async Task<ActionResult> AcceptInvite(InviteRequestViewModel viewModel)
      {
          var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId).ConfigureAwait(false);
          if (project == null)
          {
              return HttpNotFound();
          }

          if (!ModelState.IsValid)
          {
              return await Edit(viewModel.ProjectId, viewModel.ClaimId).ConfigureAwait(false);
          }

          await AccommodationInviteService.AcceptAccommodationInvite(viewModel.ProjectId, viewModel.InviteId).ConfigureAwait(false);

          return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
      }
    }
}
