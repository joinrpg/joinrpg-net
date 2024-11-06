using Joinrpg.AspNetCore.Helpers;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Accommodation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;

namespace JoinRpg.Portal.Controllers;

[Route("{ProjectId}/claim/{ClaimId}/[action]")]
public class ClaimController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    IClaimService claimService,
    IPlotRepository plotRepository,
    IClaimsRepository claimsRepository,
    IFinanceService financeService,
    ICharacterRepository characterRepository,
    IUriService uriService,
    IAccommodationRequestRepository accommodationRequestRepository,
    IAccommodationRepository accommodationRepository,
    IAccommodationInviteService accommodationInviteService,
    IAccommodationInviteRepository accommodationInviteRepository,
    IUserRepository userRepository,
    IPaymentsService paymentsService,
    IProjectMetadataRepository projectMetadataRepository,
    IProblemValidator<Claim> claimValidator) : ControllerGameBase(projectRepository, projectService, userRepository)
{
    [HttpGet("/{projectid}/character/{CharacterId}/apply")]
    [Authorize]
    public async Task<ActionResult> AddForCharacter(int projectId, int characterid)
    {
        var field = await characterRepository.GetCharacterAsync(projectId, characterid);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(projectId));
        if (field == null)
        {
            return NotFound();
        }

        return View("Add", AddClaimViewModel.Create(field, CurrentUserId, projectInfo));
    }

    [HttpGet("/{projectid}/apply")]
    [Authorize]
    public async Task<ActionResult> AddForGroup(int projectid) => await RedirectToDefaultTemplate(projectid);

    [HttpGet("/{projectid}/roles/{characterGroupId}/apply")]
    [Authorize]
    public async Task<ActionResult> AddForGroup(int projectId, int characterGroupId)
    {
        var field = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
        if (field == null)
        {
            return NotFound();
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(projectId));

        var viewModel = AddClaimViewModel.Create(field, CurrentUserId, projectInfo);

        if (viewModel.ValidationStatus.Contains(AddClaimForbideReason.NotForDirectClaims) && field.IsRoot)
        {
            return await RedirectToDefaultTemplate(projectId);
        }

        return base.View("Add", viewModel);
    }

    [HttpPost("~/{ProjectId}/claim/add")]
    [Authorize]
    public async Task<ActionResult> Add(AddClaimViewModel viewModel)
    {
        var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
        if (project == null)
        {
            return NotFound();
        }

        try
        {
            await claimService.AddClaimFromUser(viewModel.ProjectId, viewModel.CharacterGroupId, viewModel.CharacterId, viewModel.ClaimText,
                Request.GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix));

            return RedirectToAction(
              "SetupProfile",
              "Manage",
              new { checkContactsMessage = true });
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            var source = await ProjectRepository.GetClaimSource(viewModel.ProjectId, viewModel.CharacterGroupId, viewModel.CharacterId);
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(viewModel.ProjectId));
            viewModel.Fill(source, CurrentUserId, projectInfo, Request.GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix));
            return base.View(viewModel);
        }
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Edit(int projectId, int claimId)
    {
        var claim = await claimsRepository.GetClaimWithDetails(projectId, claimId).ConfigureAwait(false);
        return await ShowClaim(claim).ConfigureAwait(false);
    }

    private async Task<ActionResult> ShowClaim(Claim claim)
    {
        var error = WithClaim(claim);
        if (error != null)
        {
            return error;
        }

        var currentUser = await GetCurrentUserAsync().ConfigureAwait(false);

        var plots = claim.IsApproved && claim.Character != null
          ? await plotRepository.GetPlotsForCharacter(claim.Character).ConfigureAwait(false)
          : [];

        IEnumerable<ProjectAccommodationType>? availableAccommodation = null;
        IEnumerable<AccommodationRequest>? requestForAccommodation = null;
        IEnumerable<AccommodationPotentialNeighbors>? potentialNeighbors = null;
        IEnumerable<AccommodationInvite>? incomingInvite = null;
        IEnumerable<AccommodationInvite>? outgoingInvite = null;


        if (claim.Project.Details.EnableAccommodation)
        {
            availableAccommodation = await
                accommodationRepository.GetAccommodationForProject(claim.ProjectId).ConfigureAwait(false);
            requestForAccommodation = await accommodationRequestRepository
                .GetAccommodationRequestForClaim(claim.ClaimId).ConfigureAwait(false);
            var acceptedRequest = requestForAccommodation
                .FirstOrDefault(request => request.IsAccepted == AccommodationRequest.InviteState.Accepted);

            if (acceptedRequest != null)
            {
                var sameRequest = (await
                    accommodationRequestRepository.GetClaimsWithSameAccommodationTypeToInvite(
                        acceptedRequest.AccommodationTypeId).ConfigureAwait(false)).Where(c => c.ClaimId != claim.ClaimId)
                    .Select(c => new AccommodationPotentialNeighbors(c, NeighborType.WithSameType)); ;
                var noRequest = (await
                    accommodationRequestRepository.GetClaimsWithOutAccommodationRequest(claim.ProjectId).ConfigureAwait(false)).Select(c => new AccommodationPotentialNeighbors(c, NeighborType.NoRequest)); ;
                var currentNeighbors = (await
                   accommodationRequestRepository.GetClaimsWithSameAccommodationRequest(
                        acceptedRequest.Id).ConfigureAwait(false)).Select(c => new AccommodationPotentialNeighbors(c, NeighborType.Current));
                potentialNeighbors = sameRequest.Union(noRequest).Where(element => currentNeighbors.All(el => el.ClaimId != element.ClaimId));
            }

            incomingInvite = await accommodationInviteRepository.GetIncomingInviteForClaim(claim).ConfigureAwait(false);
            outgoingInvite = await accommodationInviteRepository.GetOutgoingInviteForClaim(claim).ConfigureAwait(false);
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(claim.ProjectId));

        var claimViewModel = new ClaimViewModel(currentUser,
            claim,
            plots,
            uriService,
            projectInfo,
            claimValidator,
            paymentsService.GetExternalPaymentUrl,
            availableAccommodation,
            requestForAccommodation,
            potentialNeighbors,
            incomingInvite,
            outgoingInvite);

        if (claim.CommentDiscussion.Comments.Any(c => !c.IsReadByUser(CurrentUserId)))
        {
            await
              claimService.UpdateReadCommentWatermark(claim.ProjectId, claim.CommentDiscussion.CommentDiscussionId,
                claim.CommentDiscussion.Comments.Max(c => c.CommentId)).ConfigureAwait(false);
        }


        var parents = claim.GetTarget().GetParentGroupsToTop();
        claimViewModel.SubscriptionTooltip =
          claimViewModel.GetFullSubscriptionTooltip(parents, currentUser.Subscriptions, claimViewModel.ClaimId);

        return View("Edit", claimViewModel);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(int projectId, int claimId, string ignoreMe)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        var error = WithClaim(claim);
        if (error != null)
        {
            return error;
        }
        try
        {
            await
              claimService.SaveFieldsFromClaim(projectId, claimId, Request.GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix));
            return RedirectToAction("Edit", "Claim", new { projectId, claimId });
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            return await Edit(projectId, claimId);
        }
    }

    [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
    public async Task<ActionResult> ApproveByMaster(int projectId, int claimId, ClaimOperationViewModel viewModel)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }

        try
        {
            await
              claimService.ApproveByMaster(claim.ProjectId, claim.ClaimId, viewModel.CommentText);

            return ReturnToClaim(projectId, claimId);
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            return await ShowClaim(claim);
        }
    }

    [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
    public async Task<ActionResult> OnHoldByMaster(int projectId, int claimId, ClaimOperationViewModel viewModel)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }

        try
        {
            await
              claimService.OnHoldByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText);

            return ReturnToClaim(projectId, claimId);
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
    public async Task<ActionResult> DeclineByMaster(int projectId, int claimId, MasterDenialOperationViewModel viewModel)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }

        try
        {
            if (!ModelState.IsValid)
            {
                return await ShowClaim(claim);
            }

            await
                claimService.DeclineByMaster(
                    claim.ProjectId,
                    claim.ClaimId,
                    (Claim.DenialStatus)viewModel.DenialStatus,
                    viewModel.CommentText,
                    viewModel.DeleteCharacter == MasterDenialExtraActionViewModel.DeleteCharacter);

            return ReturnToClaim(projectId, claimId);
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
    public async Task<ActionResult> RestoreByMaster(int projectId, int claimId, ClaimOperationViewModel viewModel, int characterId)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }

        try
        {
            if (!ModelState.IsValid)
            {
                return await ShowClaim(claim);
            }
            await
              claimService.RestoreByMaster(claim.ProjectId, claim.ClaimId, viewModel.CommentText, characterId);

            return ReturnToClaim(projectId, claimId);
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
    public async Task<ActionResult> DeclineByPlayer(int projectId, int claimId, ClaimOperationViewModel viewModel)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }
        if (claim.PlayerUserId != CurrentUserId)
        {
            return NoAccesToProjectView(claim.Project);
        }

        try
        {
            if (!ModelState.IsValid)
            {
                return await ShowClaim(claim);
            }
            await
              claimService.DeclineByPlayer(claim.ProjectId, claim.ClaimId, viewModel.CommentText);

            return ReturnToClaim(projectId, claimId);
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
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }
        try
        {
            await claimService.SetResponsible(projectId, claimId, CurrentUserId, responsibleMasterId);
            return ReturnToClaim(projectId, claimId);
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            return await ShowClaim(claim);
        }

    }

    [MasterAuthorize()]
    [HttpPost]
    public async Task<ActionResult> Move(int projectId, int claimId, ClaimOperationViewModel viewModel, int characterId)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }

        try
        {
            if (!ModelState.IsValid)
            {
                return await ShowClaim(claim);
            }

            await claimService.MoveByMaster(claim.ProjectId, claim.ClaimId, viewModel.CommentText, characterId);

            return ReturnToClaim(projectId, claimId);
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            return await ShowClaim(claim);
        }
    }

    [HttpGet("/{projectId}/myclaim")]
    [Authorize, HttpGet]
    public async Task<ActionResult> MyClaim(int projectId)
    {
        var claims = await claimsRepository.GetClaimsForPlayer(projectId, ClaimStatusSpec.Any, CurrentUserId);

        if (claims.Count == 0)
        {
            return await RedirectToDefaultTemplate(projectId);
        }

        var claimId = claims.TrySelectSingleClaim()?.ClaimId;

        return claimId != null ? ReturnToClaim(projectId, (int)claimId) : RedirectToAction("My", "ClaimList");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> FinanceOperation(int projectId, int claimId, SubmitPaymentViewModel viewModel)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
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
                financeService.FeeAcceptedOperation(new FeeAcceptedOperationRequest()
                {
                    ProjectId = claim.ProjectId,
                    ClaimId = claim.ClaimId,
                    Contents = viewModel.CommentText,
                    FeeChange = viewModel.FeeChange,
                    Money = viewModel.Money,
                    OperationDate = viewModel.OperationDate,
                    PaymentTypeId = viewModel.PaymentTypeId,
                });

            return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
        }
        catch
        {
            return await Edit(viewModel.ProjectId, viewModel.ClaimId);
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> TransferClaimPayment(PaymentTransferViewModel data)
    {
        try
        {
            await financeService.TransferPaymentAsync(
                new ClaimPaymentTransferRequest
                {
                    ProjectId = data.ProjectId,
                    ClaimId = data.ClaimId,
                    ToClaimId = data.RecipientClaimId,
                    CommentText = data.CommentText,
                    OperationDate = data.OperationDate,
                    Money = data.Money,
                });
            return RedirectToAction(
                "Edit",
                "Claim",
                new { projectId = data.ProjectId, claimId = data.ClaimId });
        }
        catch (Exception e)
        {
            return View(
                "~/Views/Payments/Error.cshtml",
                new ErrorViewModel
                {
                    Title = "Перевод между заявками",
                    Message = $"Ошибка выполнения перевода {data.Money} от заявки {data.ClaimId} к заявке {data.RecipientClaimId}",
                    Description = e.Message,
                    Data = e,
                    ReturnLink = Url.Action("Edit", "Claim", new { projectId = data.ProjectId, claimId = data.ClaimId }),
                    ReturnText = "Вернуться к заявке"
                });
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
              financeService.ChangeFee(projectid, claimid, feeValue);

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
        var claim = await claimsRepository.GetClaim(projectid, claimid);
        if (claim == null)
        {
            return NotFound();
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectid));

        var claimViewModel = new ClaimViewModel(user, claim, [], uriService, projectInfo, claimValidator, paymentsService.GetExternalPaymentUrl);

        await claimService.SubscribeClaimToUser(projectid, claimid);
        var parents = claim.GetTarget().GetParentGroupsToTop();

        var tooltip = claimViewModel.GetFullSubscriptionTooltip(parents, user.Subscriptions, claimViewModel.ClaimId);

        return Json(tooltip);
    }

    [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
    public async Task<ActionResult> Unsubscribe(int projectid, int claimid)
    {

        var user = await GetCurrentUserAsync();
        var claim = await claimsRepository.GetClaim(projectid, claimid);

        if (claim == null)
        {
            return NotFound();
        }
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectid));

        var claimViewModel = new ClaimViewModel(user, claim, [], uriService, projectInfo, claimValidator, paymentsService.GetExternalPaymentUrl);


        await claimService.UnsubscribeClaimToUser(projectid, claimid);
        var parents = claim.GetTarget().GetParentGroupsToTop();

        var tooltip = claimViewModel.GetFullSubscriptionTooltip(parents, user.Subscriptions, claimViewModel.ClaimId);

        return Json(tooltip);
    }

    private ActionResult? WithClaim(Claim claim)
    {
        if (claim == null)
        {
            return NotFound();
        }
        if (!claim.HasAccess(CurrentUserId, ExtraAccessReason.Player))
        {
            return NoAccesToProjectView(claim.Project);
        }

        return null;
    }

    [MasterAuthorize(Permission.CanManageMoney), ValidateAntiForgeryToken]
    [HttpPost]
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
                financeService.MarkPreferential(new MarkPreferentialRequest
                {
                    ProjectId = projectid,
                    ClaimId = claimid,
                    Preferential = preferential,
                });

            return RedirectToAction("Edit", "Claim", new { claimid, projectid });
        }
        catch
        {
            return await Edit(projectid, claimid);
        }
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<ActionResult> RequestPreferentialFee(int projectId, int claimId,
        MarkMeAsPreferentialViewModel viewModel)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
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
                financeService.RequestPreferentialFee(new MarkMeAsPreferentialFeeOperationRequest()
                {
                    ProjectId = claim.ProjectId,
                    ClaimId = claim.ClaimId,
                    Contents = viewModel.CommentText,
                    OperationDate = viewModel.OperationDate,
                })
                    .ConfigureAwait(false);

            return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
        }
        catch
        {
            return await Edit(viewModel.ProjectId, viewModel.ClaimId);
        }

    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<ActionResult> SetAccommodationType(AccommodationRequestViewModel viewModel)
    {
        var claim = await claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId).ConfigureAwait(false);
        if (claim == null)
        {
            return NotFound();
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

            _ = await claimService.SetAccommodationType(
                viewModel.ProjectId,
                viewModel.ClaimId,
                viewModel.AccommodationTypeId)
                .ConfigureAwait(false);

            return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
        }
        catch
        {
            return await Edit(viewModel.ProjectId, viewModel.ClaimId).ConfigureAwait(false);
        }
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> LeaveGroupAsync(int projectId, int claimId)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim is null)
        {
            return NotFound();
        }
        var error = WithClaim(claim);
        if (error is not null)
        {
            return error;
        }
        try
        {
            if (!ModelState.IsValid)
            {
                return await Edit(projectId, claimId);
            }

            await claimService.LeaveAccommodationGroupAsync(projectId, claimId);
            return RedirectToAction("Edit", "Claim", new { projectId, claimId });
        }
        catch
        {
            return await Edit(projectId, claimId);
        }
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<ActionResult> Invite(InviteRequestViewModel viewModel)
    {
        var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId).ConfigureAwait(false);
        if (project == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return await Edit(viewModel.ProjectId, viewModel.ClaimId).ConfigureAwait(false);
        }

        _ = await accommodationInviteService.CreateAccommodationInviteToGroupOrClaim(viewModel.ProjectId,
            viewModel.ClaimId,
            viewModel.ReceiverClaimOrAccommodationRequest,
            viewModel.RequestId,
           InviteRequestViewModel.AccommodationRequestPrefix).ConfigureAwait(false);

        return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
    }

    private async Task<IActionResult> InviteActionAsync(
        int projectId,
        int claimId,
        int inviteId,
        AccommodationRequest.InviteState inviteState)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return await Edit(projectId, claimId);
        }

        switch (inviteState)
        {
            case AccommodationRequest.InviteState.Canceled:
            case AccommodationRequest.InviteState.Declined:
                await accommodationInviteService.CancelOrDeclineAccommodationInvite(
                    inviteId,
                    inviteState);
                break;

            case AccommodationRequest.InviteState.Accepted:
                await accommodationInviteService.AcceptAccommodationInvite(projectId, inviteId);
                break;
        }

        return RedirectToAction("Edit", "Claim", new { projectId, claimId });
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public Task<IActionResult> CancelInviteAsync(int projectId, int claimId, int inviteId)
        => InviteActionAsync(projectId, claimId, inviteId, AccommodationRequest.InviteState.Canceled);

    [ValidateAntiForgeryToken]
    [HttpPost]
    public Task<IActionResult> DeclineInviteAsync(int projectId, int claimId, int inviteId)
        => InviteActionAsync(projectId, claimId, inviteId, AccommodationRequest.InviteState.Declined);

    [ValidateAntiForgeryToken]
    [HttpPost]
    public Task<IActionResult> AcceptInviteAsync(int projectId, int claimId, int inviteId)
        => InviteActionAsync(projectId, claimId, inviteId, AccommodationRequest.InviteState.Accepted);

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> TransferClaimPayment(int projectId, int claimId)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }
        var error = WithClaim(claim);
        if (error != null)
        {
            return error;
        }


        var claims = await claimsRepository.GetClaimsForMoneyTransfersListAsync(
            claim.ProjectId,
            ClaimStatusSpec.ActiveOrOnHold);
        if (claims.Count == 0 || (claims.Count == 1 && claims.First().ClaimId == claimId))
        {
            return View(
                "~/Views/Payments/Error.cshtml",
                new ErrorViewModel
                {
                    Title = "Ошибка",
                    Message = "Невозможно выполнить перевод, так как нет активных или отложенных заявок",
                    ReturnLink = Url.Action("Edit", "Claim", new { projectId, claimId }),
                    ReturnText = "Вернуться к заявке"
                });
        }

        return View("PaymentTransfer", new PaymentTransferViewModel(claim, claims));
    }

    private async Task<ActionResult> RedirectToDefaultTemplate(int projectid)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(projectid));

        if (projectInfo.DefaultTemplateCharacter is not null)
        {
            return RedirectToAction("AddForCharacter", new { projectid, projectInfo.DefaultTemplateCharacter.CharacterId });
        }

        //TODO Start of HACKS remove when groups claims will die
        var project = await ProjectRepository.GetProjectAsync(projectid);

        if (project.RootGroup.HaveDirectSlots)
        {
            return RedirectToAction("AddForGroup", new { projectid, project.RootGroup.CharacterGroupId });
        }

        var childSlots = project.RootGroup.Characters.Where(c => c.CharacterType == CharacterType.Slot && c.IsActive).ToList();
        if (childSlots.Count == 1 && childSlots.Single() is Character targetSlot && targetSlot.IsAcceptingClaims())
        {
            return RedirectToAction("AddForCharacter", new { projectid, childSlots.Single().CharacterId });
        }

        //TODO end of hacks

        return Redirect($"/{projectInfo.ProjectId.Value}/default-slot-not-set");
    }

    private ActionResult ReturnToClaim(int projectId, int claimId) => RedirectToAction("Edit", "Claim", new { claimId, projectId });
}
