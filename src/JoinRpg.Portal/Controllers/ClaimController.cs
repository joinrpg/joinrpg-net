using System.Diagnostics.CodeAnalysis;
using Joinrpg.AspNetCore.Helpers;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Accommodation;
using JoinRpg.Web.ProjectMasterTools.Subscribe;
using JoinRpg.WebPortal.Managers.Plots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{ProjectId}/claim/{ClaimId}/[action]")]
public class ClaimController(
    IProjectRepository ProjectRepository,
    IClaimService claimService,
    IGameSubscribeClient gameSubscribeClient,
    IClaimsRepository claimsRepository,
    IFinanceService financeService,
    ICharacterRepository characterRepository,
    IAccommodationRequestRepository accommodationRequestRepository,
    IAccommodationRepository accommodationRepository,
    IAccommodationInviteService accommodationInviteService,
    IAccommodationInviteRepository accommodationInviteRepository,
    IUserRepository UserRepository,
    IPaymentsService paymentsService,
    IProjectMetadataRepository projectMetadataRepository,
    IProblemValidator<Claim> claimValidator,
    ICurrentUserAccessor currentUserAccessor,
    CharacterPlotViewService characterPlotViewService
    ) : JoinControllerGameBase
{
    [HttpGet("/{projectid}/character/{CharacterId}/apply")]
    [Authorize]
    public async Task<ActionResult> AddForCharacter(ProjectIdentification projectId, int characterid)
    {
        var field = await characterRepository.GetCharacterAsync(projectId, characterid);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var userInfo = await UserRepository.GetRequiredUserInfo(currentUserAccessor.UserIdentification);
        if (field == null)
        {
            return NotFound();
        }

        return View("Add", AddClaimViewModel.Create(field, userInfo, projectInfo));
    }

    [HttpGet("/{projectid}/apply")]
    [Authorize]
    public async Task<ActionResult> AddForGroup(int projectid) => await RedirectToDefaultTemplate(projectid);

    [HttpGet("/{projectid}/roles/{characterGroupId}/apply")]
    [Authorize]
    public async Task<ActionResult> AddForGroup(int projectId, int characterGroupId) => await RedirectToDefaultTemplate(projectId);

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
            await claimService.AddClaimFromUser(new CharacterIdentification(viewModel.ProjectId, viewModel.CharacterId),
                viewModel.ClaimText, Request.GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix), viewModel.SensitiveDataAllowed);

            return RedirectToAction(
              "SetupProfile",
              "Manage",
              new { checkContactsMessage = true });
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            var userInfo = await UserRepository.GetRequiredUserInfo(currentUserAccessor.UserIdentification);
            var source = await characterRepository.GetCharacterAsync(viewModel.ProjectId, viewModel.CharacterId);
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(viewModel.ProjectId));
            viewModel.Fill(source, userInfo, projectInfo, Request.GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix));
            return base.View(viewModel);
        }
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Edit(int projectId, int claimId)
    {
        var claim = await claimsRepository.GetClaimWithDetails(projectId, claimId);
        return await ShowClaim(claim);
    }

    private async Task<ActionResult> ShowClaim(Claim claim)
    {
        var error = WithClaim(claim);
        if (error != null)
        {
            return error;
        }

        var plots = await characterPlotViewService.GetPlotsForCharacter(new CharacterIdentification(claim.ProjectId, claim.CharacterId));


        var accommodationModel = claim.Project.Details.EnableAccommodation ? await ShowAccommodationModel(claim) : null;

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(claim.ProjectId));

        var userInfo = await UserRepository.GetRequiredUserInfo(new UserIdentification(claim.PlayerUserId));

        var claimViewModel = new ClaimViewModel(currentUserAccessor,
            claim,
            plots,
            projectInfo,
            claimValidator,
            paymentsService.GetExternalPaymentUrl,
            accommodationModel,
            userInfo)
        {
            SubscriptionTooltip = await gameSubscribeClient.GetSubscribeForClaim(claim.ProjectId, claim.ClaimId),
        };

        if (claim.CommentDiscussion.Comments.Any(c => !c.IsReadByUser(currentUserAccessor.UserId)))
        {
            await
              claimService.UpdateReadCommentWatermark(claim.ProjectId, claim.CommentDiscussion.CommentDiscussionId,
                claim.CommentDiscussion.Comments.Max(c => c.CommentId)).ConfigureAwait(false);
        }

        return View("Edit", claimViewModel);
    }

    private async Task<ClaimAccommodationViewModel> ShowAccommodationModel(Claim claim)
    {
        var availableAccommodation = await
            accommodationRepository.GetAccommodationForProject(claim.ProjectId).ConfigureAwait(false);
        var requestForAccommodation = await accommodationRequestRepository
            .GetAccommodationRequestForClaim(claim.ClaimId).ConfigureAwait(false);
        var acceptedRequest = requestForAccommodation
            .FirstOrDefault(request => request.IsAccepted == AccommodationRequest.InviteState.Accepted);

        var incomingInvite = await accommodationInviteRepository.GetIncomingInviteForClaim(claim);
        var outgoingInvite = await accommodationInviteRepository.GetOutgoingInviteForClaim(claim);

        IEnumerable<AccommodationPotentialNeighbors>? potentialNeighbors = null;

        if (acceptedRequest != null)
        {
            var sameRequest = (await
                accommodationRequestRepository.GetClaimsWithSameAccommodationTypeToInvite(
                    acceptedRequest.AccommodationTypeId).ConfigureAwait(false)).Where(c => c.ClaimId != claim.ClaimId)
                .Select(c => new AccommodationPotentialNeighbors(c, NeighborType.WithSameType));
            var noRequest = (await
                accommodationRequestRepository.GetClaimsWithOutAccommodationRequest(claim.ProjectId).ConfigureAwait(false)).Select(c => new AccommodationPotentialNeighbors(c, NeighborType.NoRequest)); ;
            var currentNeighbors = (await
               accommodationRequestRepository.GetClaimsWithSameAccommodationRequest(
                    acceptedRequest.Id)).Select(c => c.ClaimId);
            potentialNeighbors = sameRequest.Union(noRequest).Where(element => !currentNeighbors.Contains(element.ClaimId));

            incomingInvite = incomingInvite.Where(i => !currentNeighbors.Contains(i.FromClaimId)).ToList();
            outgoingInvite = outgoingInvite.Where(i => !currentNeighbors.Contains(i.ToClaimId)).ToList();
        }
        else
        {
            potentialNeighbors = [];
        }

        var claimAccommodationViewModel = new ClaimAccommodationViewModel(availableAccommodation,
        potentialNeighbors,
        incomingInvite,
        outgoingInvite,
        claim,
        currentUserAccessor);
        return claimAccommodationViewModel;
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(int projectId, int claimId, string ignoreMe)
    {
        var claimIdentification = new ClaimIdentification(projectId, claimId);
        var claim = await claimsRepository.GetClaim(claimIdentification);
        var error = WithClaim(claim);
        if (error != null)
        {
            return error;
        }
        try
        {
            await
              claimService.SaveFieldsFromClaim(claimIdentification, Request.GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix));
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
              claimService.ApproveByMaster(claim.GetId(), viewModel.CommentText);

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
              claimService.OnHoldByMaster(claim.GetId(), viewModel.CommentText);

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
                    claim.GetId(),
                    (ClaimDenialReason)viewModel.DenialStatus,
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
              claimService.RestoreByMaster(claim.GetId(), viewModel.CommentText, new CharacterIdentification(projectId, characterId));

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
        if (claim.PlayerUserId != currentUserAccessor.UserId)
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
              claimService.DeclineByPlayer(claim.GetId(), viewModel.CommentText);

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
    public async Task<ActionResult> ChangeResponsible(int projectId, int claimId, UserIdentification responsibleMasterId)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }
        try
        {
            await claimService.SetResponsible(claim.GetId(), responsibleMasterId);
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
    public async Task<ActionResult> Move(int projectId, int claimId, MoveClaimOperationViewModel viewModel, int characterId)
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

            await claimService.MoveByMaster(claim.GetId(), viewModel.CommentText, new CharacterIdentification(projectId, characterId));

            if (viewModel.AcceptAfterMove)
            {
                await claimService.ApproveByMaster(claim.GetId(), "");
            }

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
        var claims = await claimsRepository.GetClaimsForPlayer(projectId, ClaimStatusSpec.Any, currentUserAccessor.UserIdentification);

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
                    ClaimId = claim.ClaimId,
                    Contents = viewModel.CommentText,
                    FeeChange = viewModel.FeeChange,
                    Money = viewModel.Money,
                    OperationDate = viewModel.OperationDate,
                    PaymentTypeId = new PrimitiveTypes.ProjectMetadata.Payments.PaymentTypeIdentification(viewModel.ProjectId, viewModel.PaymentTypeId),
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
              financeService.ChangeFee(new ClaimIdentification(projectid, claimid), feeValue);

            return RedirectToAction("Edit", "Claim", new { claimid, projectid });
        }
        catch
        {
            return await Edit(projectid, claimid);
        }
    }

    //TODO перенести эти операции в WebApi контроллер
    [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
    public async Task<ActionResult> Subscribe(int projectid, int claimid)
    {
        var claim = await claimsRepository.GetClaim(projectid, claimid);
        if (claim == null)
        {
            return NotFound();
        }

        await claimService.SubscribeClaimToUser(projectid, claimid);

        var tooltip = await gameSubscribeClient.GetSubscribeForClaim(projectid, claimid);

        return Json(tooltip);
    }

    //TODO перенести эти операции в WebApi контроллер
    [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
    public async Task<ActionResult> Unsubscribe(int projectid, int claimid)
    {
        var claim = await claimsRepository.GetClaim(projectid, claimid);

        if (claim == null)
        {
            return NotFound();
        }

        await claimService.UnsubscribeClaimToUser(projectid, claimid);

        var tooltip = await gameSubscribeClient.GetSubscribeForClaim(projectid, claimid);

        return Json(tooltip);
    }

    private ActionResult? WithClaim(Claim claim)
    {
        if (claim == null)
        {
            return NotFound();
        }
        if (!claim.HasAccess(currentUserAccessor.UserId, Permission.None, ExtraAccessReason.Player))
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
        var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
        if (project == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return await Edit(viewModel.ProjectId, viewModel.ClaimId);
        }

        _ = await accommodationInviteService.CreateAccommodationInviteToGroupOrClaim(viewModel.ProjectId,
            viewModel.ClaimId,
            viewModel.ReceiverClaimOrAccommodationRequest,
            viewModel.RequestId);

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

        return Redirect($"/{projectInfo.ProjectId.Value}/default-slot-not-set");
    }

    private RedirectToActionResult ReturnToClaim(int projectId, int claimId) => RedirectToAction("Edit", "Claim", new { claimId, projectId });

    [DoesNotReturn]
    protected ActionResult NoAccesToProjectView(Project project) => throw new NoAccessToProjectException(project, currentUserAccessor.UserId);
}
