using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Joinrpg.AspNetCore.Helpers;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Accommodation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Controllers
{
    [Route("{ProjectId}/claim/{ClaimId}/[action]")]
    public class ClaimController : ControllerGameBase
    {
        private readonly IClaimService _claimService;
        private readonly IPlotRepository _plotRepository;
        private readonly IClaimsRepository _claimsRepository;
        private readonly IAccommodationRequestRepository _accommodationRequestRepository;
        private readonly IAccommodationRepository _accommodationRepository;
        private IAccommodationInviteService AccommodationInviteService { get; }
        private IFinanceService FinanceService { get; }
        private ICharacterRepository CharacterRepository { get; }
        private IAccommodationInviteRepository AccommodationInviteRepository { get; }
        private IUriService UriService { get; }

        [HttpGet("/{projectid}/character/{CharacterId}/apply")]
        [Authorize]
        public async Task<ActionResult> AddForCharacter(int projectid, int characterid)
        {
            var field = await CharacterRepository.GetCharacterAsync(projectid, characterid);
            if (field == null)
            {
                return NotFound();
            }

            return View("Add", AddClaimViewModel.Create(field, CurrentUserId));
        }

        [HttpGet("/{projectid}/apply")]
        [Authorize]
        public async Task<ActionResult> AddForGroup(int projectid)
        {
            //TODO remove redirect here
            var project = await ProjectRepository.GetProjectAsync(projectid);
            return RedirectToAction("AddForGroup",
                new { project.ProjectId, project.RootGroup.CharacterGroupId });
        }

        [HttpGet("/{projectid}/roles/{characterGroupId}/apply")]
        [Authorize]
        public async Task<ActionResult> AddForGroup(int projectid, int characterGroupId)
        {
            var field = await ProjectRepository.GetGroupAsync(projectid, characterGroupId);
            if (field == null)
            {
                return NotFound();
            }

            return View("Add", AddClaimViewModel.Create(field, CurrentUserId));
        }

        public ClaimController(
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
            IUserRepository userRepository)
            : base(projectRepository, projectService, userRepository)
        {
            _claimService = claimService;
            _plotRepository = plotRepository;
            _claimsRepository = claimsRepository;
            _accommodationRequestRepository = accommodationRequestRepository;
            _accommodationRepository = accommodationRepository;
            AccommodationInviteService = accommodationInviteService;
            AccommodationInviteRepository = accommodationInviteRepository;
            FinanceService = financeService;
            CharacterRepository = characterRepository;
            UriService = uriService;
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
                await _claimService.AddClaimFromUser(viewModel.ProjectId, viewModel.CharacterGroupId, viewModel.CharacterId, viewModel.ClaimText,
                    Request.GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix));

                return RedirectToAction(
                  "SetupProfile",
                  "Manage",
                  new { checkContactsMessage = true });
            }
            catch (Exception exception)
            {
                ModelState.AddException(exception);
                var source = await ProjectRepository.GetClaimSource(viewModel.ProjectId, viewModel.CharacterGroupId, viewModel.CharacterId).ConfigureAwait(false);
                return View(viewModel.Fill(source, CurrentUserId));
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

            var currentUser = await GetCurrentUserAsync().ConfigureAwait(false);

            var plots = claim.IsApproved && claim.Character != null
              ? await _plotRepository.GetPlotsForCharacter(claim.Character).ConfigureAwait(false)
              : Array.Empty<PlotElement>();

            IEnumerable<ProjectAccommodationType>? availableAccommodation = null;
            IEnumerable<AccommodationPotentialNeighbors>? potentialNeighbors = null;
            IEnumerable<AccommodationInvite>? incomingInvite = null;
            IEnumerable<AccommodationInvite>? outgoingInvite = null;

            if (claim.Project.Details.EnableAccommodation)
            {
                availableAccommodation = await _accommodationRepository
                    .GetAccommodationForProject(claim.ProjectId)
                    .ConfigureAwait(false);
                var accommodationRequests = await _accommodationRequestRepository
                    .GetAccommodationRequestForClaim(claim.ClaimId)
                    .ConfigureAwait(false);
                var acceptedRequest = accommodationRequests
                    .FirstOrDefault(request => request.IsAccepted == AccommodationRequest.InviteState.Accepted);
                var acceptedRequestId = acceptedRequest?.Id;
                var acceptedRequestAccommodationTypeId = acceptedRequest?.AccommodationTypeId;

                if (acceptedRequestId.HasValue && acceptedRequestAccommodationTypeId.HasValue)
                {
                    var sameRequest = (await _accommodationRequestRepository
                            .GetClaimsWithSameAccommodationTypeToInvite(
                                acceptedRequestAccommodationTypeId.Value)
                            .ConfigureAwait(false))
                        .Where(c => c.ClaimId != claim.ClaimId)
                        .Select(
                            c => new AccommodationPotentialNeighbors(c, NeighborType.WithSameType));
                    var noRequest = (await _accommodationRequestRepository
                            .GetClaimsWithOutAccommodationRequest(claim.ProjectId)
                            .ConfigureAwait(false))
                        .Select(
                            c => new AccommodationPotentialNeighbors(c, NeighborType.NoRequest));
                    var currentNeighbors = (await _accommodationRequestRepository
                            .GetClaimsWithSameAccommodationRequest(acceptedRequestId.Value)
                            .ConfigureAwait(false))
                        .Select(c => new AccommodationPotentialNeighbors(c, NeighborType.Current));
                    potentialNeighbors = sameRequest
                        .Union(noRequest)
                        .Where(element => currentNeighbors
                            .All(el => el.ClaimId != element.ClaimId));
                }

                incomingInvite = await AccommodationInviteRepository.GetIncomingInviteForClaim(claim).ConfigureAwait(false);
                outgoingInvite = await AccommodationInviteRepository.GetOutgoingInviteForClaim(claim).ConfigureAwait(false);
            }

            var ps = HttpContext.RequestServices.GetRequiredService<IPaymentsService>();

            var claimViewModel = new ClaimViewModel(
                currentUser,
                claim,
                plots,
                UriService,
                ps.GetConfiguredMethods(),
                availableAccommodation,
                potentialNeighbors,
                incomingInvite,
                outgoingInvite);

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
                  _claimService.SaveFieldsFromClaim(projectId, claimId, Request.GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix));
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
            var claim = await _claimsRepository.GetClaim(projectId, claimId);
            if (claim == null)
            {
                return NotFound();
            }

            try
            {
                await
                  _claimService.ApproveByMaster(claim.ProjectId, claim.ClaimId, viewModel.CommentText);

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
            var claim = await _claimsRepository.GetClaim(projectId, claimId);
            if (claim == null)
            {
                return NotFound();
            }

            try
            {
                await
                  _claimService.OnHoldByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText);

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
            var claim = await _claimsRepository.GetClaim(projectId, claimId);
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
                    _claimService.DeclineByMaster(
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
        public async Task<ActionResult> RestoreByMaster(int projectId, int claimId, ClaimOperationViewModel viewModel)
        {
            var claim = await _claimsRepository.GetClaim(projectId, claimId);
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
                  _claimService.RestoreByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText);

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
            var claim = await _claimsRepository.GetClaim(projectId, claimId);
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
                  _claimService.DeclineByPlayer(claim.ProjectId, claim.ClaimId, viewModel.CommentText);

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
            var claim = await _claimsRepository.GetClaim(projectId, claimId);
            if (claim == null)
            {
                return NotFound();
            }
            try
            {
                await _claimService.SetResponsible(projectId, claimId, CurrentUserId, responsibleMasterId);
                return ReturnToClaim(projectId, claimId);
            }
            catch (Exception exception)
            {
                ModelState.AddException(exception);
                return await ShowClaim(claim);
            }

        }

        /// <param name="projectId"></param>
        /// <param name="claimId"></param>
        /// <param name="viewModel"></param>
        /// <param name="claimTarget">Note that name is hardcoded in view. (TODO improve)</param>
        [MasterAuthorize()]
        [HttpPost]
        public async Task<ActionResult> Move(int projectId, int claimId, ClaimOperationViewModel viewModel, string claimTarget)
        {
            var claim = await _claimsRepository.GetClaim(projectId, claimId);
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
                var characterGroupId = claimTarget.UnprefixNumber(CharacterAndGroupPrefixer.GroupFieldPrefix);
                var characterId = claimTarget.UnprefixNumber(CharacterAndGroupPrefixer.CharFieldPrefix);
                await
                  _claimService.MoveByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText, characterGroupId, characterId);

                return ReturnToClaim(projectId, claimId);
            }
            catch (Exception exception)
            {
                ModelState.AddException(exception);
                return await ShowClaim(claim);
            }
        }

        [MustUseReturnValue]
        private ActionResult ReturnToClaim(int projectId, int claimId) => RedirectToAction("Edit", "Claim", new { claimId, projectId });

        [HttpGet("/{projectId}/myclaim")]
        [Authorize, HttpGet]
        public async Task<ActionResult> MyClaim(int projectId)
        {
            var claims = await _claimsRepository.GetClaimsForPlayer(projectId, ClaimStatusSpec.Any, CurrentUserId);

            if (claims.Count == 0)
            {
                var project = await ProjectRepository.GetProjectAsync(projectId);
                return RedirectToAction("AddForGroup", new { projectId, project.RootGroup.CharacterGroupId });
            }

            var claimId = claims.TrySelectSingleClaim()?.ClaimId;

            return claimId != null ? ReturnToClaim(projectId, (int)claimId) : RedirectToAction("My", "ClaimList");
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> FinanceOperation(int projectId, int claimId, SubmitPaymentViewModel viewModel)
        {
            var claim = await _claimsRepository.GetClaim(projectId, claimId);
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
                await FinanceService.TransferPaymentAsync(
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
                return View("Error",
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
                return NotFound();
            }

            var claimViewModel = new ClaimViewModel(user, claim, Array.Empty<PlotElement>(), UriService);

            await _claimService.SubscribeClaimToUser(projectid, claimid);
            var parents = claim.GetTarget().GetParentGroupsToTop();

            var tooltip = claimViewModel.GetFullSubscriptionTooltip(parents, user.Subscriptions, claimViewModel.ClaimId);

            return Json(tooltip);
        }

        [HttpPost, MasterAuthorize(), ValidateAntiForgeryToken]
        public async Task<ActionResult> Unsubscribe(int projectid, int claimid)
        {

            var user = await GetCurrentUserAsync();
            var claim = await _claimsRepository.GetClaim(projectid, claimid);

            if (claim == null)
            {
                return NotFound();
            }

            var claimViewModel = new ClaimViewModel(user, claim, Array.Empty<PlotElement>(), UriService);


            await _claimService.UnsubscribeClaimToUser(projectid, claimid);
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
                    FinanceService.MarkPreferential(new MarkPreferentialRequest
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
            var claim = await _claimsRepository.GetClaim(projectId, claimId);
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
                    FinanceService.RequestPreferentialFee(new MarkMeAsPreferentialFeeOperationRequest()
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
            var claim = await _claimsRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId).ConfigureAwait(false);
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

                _ = await _claimService.SetAccommodationType(
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
            var claim = await _claimsRepository.GetClaim(projectId, claimId);
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

                await _claimService.LeaveAccommodationGroupAsync(projectId, claimId);
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

            _ = await AccommodationInviteService.CreateAccommodationInviteToGroupOrClaim(viewModel.ProjectId,
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
            var claim = await _claimsRepository.GetClaim(projectId, claimId);
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
                    await AccommodationInviteService.CancelOrDeclineAccommodationInvite(
                        inviteId,
                        inviteState);
                    break;

                case AccommodationRequest.InviteState.Accepted:
                    await AccommodationInviteService.AcceptAccommodationInvite(projectId, inviteId);
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
            var claim = await _claimsRepository.GetClaim(projectId, claimId);
            if (claim == null)
            {
                return NotFound();
            }
            var error = WithClaim(claim);
            if (error != null)
            {
                return error;
            }


            IReadOnlyCollection<Claim> claims = await _claimsRepository.GetClaimsForMoneyTransfersListAsync(
                claim.ProjectId,
                ClaimStatusSpec.ActiveOrOnHold);
            if (claims.Count == 0 || (claims.Count == 1 && claims.First().ClaimId == claimId))
            {
                return View("Error", new ErrorViewModel
                {
                    Title = "Ошибка",
                    Message = "Невозможно выполнить перевод, так как нет активных или отложенных заявок",
                    ReturnLink = Url.Action("Edit", "Claim", new { projectId, claimId }),
                    ReturnText = "Вернуться к заявке"
                });
            }

            return View("PaymentTransfer", new PaymentTransferViewModel(claim, claims));
        }
    }
}
