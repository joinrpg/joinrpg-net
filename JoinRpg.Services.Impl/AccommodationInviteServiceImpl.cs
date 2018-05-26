using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl
{
    [UsedImplicitly]
    public class AccommodationInviteServiceImpl : DbServiceImplBase, IAccommodationInviteService
    {
        public AccommodationInviteServiceImpl(IUnitOfWork unitOfWork, IEmailService emailService) :
            base(unitOfWork)
        {
            EmailService = emailService;
        }

        private IEmailService EmailService { get; }

        private async Task<AccommodationInvite> CreateAccommodationInvite(int projectId,
            int senderClaimId,
            int receiverClaimId,
            int accommodationRequestId)
        {
            //todo: make null result descriptive

            var receiverCurrentAccommodationRequest = await UnitOfWork
                .GetDbSet<Claim>()
                .Where(claim => claim.ClaimId == receiverClaimId)
                .Select(claim => claim.AccommodationRequest)
                .Include(request => request.Subjects)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            var senderAccommodationRequest = await UnitOfWork.GetDbSet<AccommodationRequest>()
                .Where(request => request.Id == accommodationRequestId)
                .Include(request => request.Subjects)
                .Include(request => request.AccommodationType)
                .Include(c => c.Project)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            //we not allow invitation to/from already settled members
            if (receiverCurrentAccommodationRequest?.AccommodationId != null ||
                senderAccommodationRequest?.AccommodationId != null)
            {
                return null;
            }

            //invite only claims with same type of room, or claims with out room type at all
            if (receiverCurrentAccommodationRequest?.AccommodationTypeId !=
                senderAccommodationRequest?.AccommodationTypeId &&
                receiverCurrentAccommodationRequest?.AccommodationTypeId != null)
            {
                return null;
            }

            var newDwellersCount = receiverCurrentAccommodationRequest?.Subjects.Count ?? 1;
            var canInvite = senderAccommodationRequest.Subjects.Count + newDwellersCount <=
                            senderAccommodationRequest.AccommodationType.Capacity;
            canInvite = canInvite &&
                        (senderAccommodationRequest.AccommodationTypeId ==
                         receiverCurrentAccommodationRequest?.AccommodationTypeId ||
                         receiverCurrentAccommodationRequest == null);
            if (!canInvite)
            {
                return null;
            }

            var inviteRequest = new AccommodationInvite
            {
                ProjectId = projectId,
                FromClaimId = senderClaimId,
                ToClaimId = receiverClaimId,
                IsAccepted = AccommodationRequest.InviteState.Unanswered,
            };

            UnitOfWork.GetDbSet<AccommodationInvite>().Add(inviteRequest);
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);
            //todo email it

            var receiver = await UnitOfWork
                .GetDbSet<Claim>()
                .Where(claim => claim.ClaimId == receiverClaimId)
                .ToArrayAsync().ConfigureAwait(false);

            await EmailService
                .Email(await CreateInviteEmail<NewInviteEmail>(receiver,
                    senderAccommodationRequest.Project).ConfigureAwait(false))
                .ConfigureAwait(false);

            return inviteRequest;
        }

        private async Task<T> CreateInviteEmail<T>(Claim[] recipients, Project project)
            where T : InviteEmailModel, new()
        {
            return new T()
            {
                Initiator = await GetCurrentUser().ConfigureAwait(false),
                ProjectName = project.ProjectName,
                Recipients = recipients.GetInviteSubscriptions(),
                RecipientClaims = recipients,
                Text = new MarkdownString(),
            };
        }

        private async Task<IEnumerable<AccommodationInvite>>
            CreateAccommodationInviteToAccommodationRequest(int projectId,
                int senderClaimId,
                int receiverAccommodationRequestId,
                int accommodationRequestId)
        {
            //todo: make null result descriptive

            var receiverCurrentAccommodationRequest = await UnitOfWork
                .GetDbSet<AccommodationRequest>()
                .Where(request => request.Id == receiverAccommodationRequestId)
                .Include(request => request.Subjects)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            var senderAccommodationRequest = await UnitOfWork.GetDbSet<AccommodationRequest>()
                .Where(request => request.Id == accommodationRequestId)
                .Include(request => request.Subjects)
                .Include(request => request.AccommodationType)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            //we not allow invitation to/from already settled members
            if (receiverCurrentAccommodationRequest?.AccommodationId != null ||
                senderAccommodationRequest?.AccommodationId != null)
            {
                return null;
            }

            //invite only claims with same type of room, or claims with out room type at all
            if (receiverCurrentAccommodationRequest?.AccommodationTypeId !=
                senderAccommodationRequest?.AccommodationTypeId &&
                receiverCurrentAccommodationRequest?.AccommodationTypeId != null)
            {
                return null;
            }

            var newDwellersCount = receiverCurrentAccommodationRequest?.Subjects.Count ?? 1;
            var canInvite = senderAccommodationRequest?.Subjects.Count + newDwellersCount <=
                            senderAccommodationRequest?.AccommodationType.Capacity;
            canInvite = canInvite &&
                        (senderAccommodationRequest.AccommodationTypeId ==
                         receiverCurrentAccommodationRequest?.AccommodationTypeId ||
                         receiverCurrentAccommodationRequest == null);
            if (!canInvite)
            {
                return null;
            }

            var receiversClaims = await UnitOfWork
                .GetDbSet<Claim>()
                .Where(claim => claim.AccommodationRequest_Id == receiverAccommodationRequestId)
                .Include(c => c.Player)
                .ToArrayAsync()
                .ConfigureAwait(false);
            var result = new List<AccommodationInvite>();
            foreach (var receiverClaim in receiversClaims)
            {
                var inviteRequest = new AccommodationInvite
                {
                    ProjectId = projectId,
                    FromClaimId = senderClaimId,
                    ToClaimId = receiverClaim.ClaimId,
                    IsAccepted = AccommodationRequest.InviteState.Unanswered,
                };

                UnitOfWork.GetDbSet<AccommodationInvite>().Add(inviteRequest);
                result.Add(inviteRequest);
            }

            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

            await EmailService
                .Email(await CreateInviteEmail<NewInviteEmail>(receiversClaims,
                    senderAccommodationRequest.Project).ConfigureAwait(false))
                .ConfigureAwait(false);
            return result;
        }

        public async Task<IEnumerable<AccommodationInvite>> CreateAccommodationInviteToGroupOrClaim(
            int projectId,
            int senderClaimId,
            string receiverClaimOrAccommodationRequestId,
            int accommodationRequestId,
            string accommodationRequestPrefix)
        {
            if (receiverClaimOrAccommodationRequestId.StartsWith(accommodationRequestPrefix))
                return await CreateAccommodationInviteToAccommodationRequest(projectId,
                    senderClaimId,
                    int.Parse(receiverClaimOrAccommodationRequestId.Substring(2)),
                    accommodationRequestId).ConfigureAwait(false);

            return new[]
            {
                await CreateAccommodationInvite(projectId,
                    senderClaimId,
                    int.Parse(receiverClaimOrAccommodationRequestId),
                    accommodationRequestId).ConfigureAwait(false),
            };
        }


        public async Task<AccommodationInvite> AcceptAccommodationInvite(int projectId,
            int inviteId)
        {
            //todo: make null result descriptive
            var inviteRequest = await UnitOfWork.GetDbSet<AccommodationInvite>()
                .Where(invite => invite.Id == inviteId)
                .Include(invite => invite.To)
                .Include(invite => invite.From)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            var receiverAccommodationRequest =
                await GetAccommodationRequestByClaim(inviteRequest.ToClaimId).ConfigureAwait(false);
            var senderAccommodationRequest =
                await GetAccommodationRequestByClaim(inviteRequest.FromClaimId)
                    .ConfigureAwait(false);

            var roomFreeSpace = (senderAccommodationRequest.AccommodationId != null)
                ? senderAccommodationRequest.Accommodation.GetRoomFreeSpace()
                : senderAccommodationRequest.GetAbstractRoomFreeSpace();


            var canInvite = roomFreeSpace >= (receiverAccommodationRequest?.Subjects.Count ?? 0);

            if (!canInvite)
            {
                return null;
            }

            receiverAccommodationRequest?.Subjects.Remove(inviteRequest.To);
            senderAccommodationRequest.Subjects.Add(inviteRequest.To);
            inviteRequest.To.AccommodationRequest = senderAccommodationRequest;

            if (receiverAccommodationRequest != null)
            {
                // ReSharper disable once PossibleNullReferenceException
                foreach (var claim in receiverAccommodationRequest?.Subjects)
                {
                    await DeclineOtherInvite(claim.ClaimId, inviteId).ConfigureAwait(false);
                    senderAccommodationRequest.Subjects.Add(claim);
                }

                UnitOfWork.GetDbSet<AccommodationRequest>().Remove(receiverAccommodationRequest);
            }

            inviteRequest.IsAccepted = AccommodationRequest.InviteState.Accepted;
            inviteRequest.ResolveDescription = ResolveDescription.Accepted;
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);


            var receivers = await UnitOfWork.GetDbSet<Claim>()
                .Where(claim => inviteRequest.FromClaimId == claim.ClaimId)
                .Include(claim => claim.Player)
                .ToArrayAsync()
                .ConfigureAwait(false);

            await EmailService
                .Email(await CreateInviteEmail<AcceptInviteEmail>(receivers,
                    inviteRequest.Project).ConfigureAwait(false))
                .ConfigureAwait(false);


            return inviteRequest;
        }

        public async Task<AccommodationInvite> CancelOrDeclineAccommodationInvite(int inviteId,
            AccommodationRequest.InviteState newState)
        {
            var acceptedStates = new[]
            {
                AccommodationRequest.InviteState.Declined, AccommodationRequest.InviteState.Canceled,
            };

            if (!acceptedStates.Contains(newState))
            {
                return null;
            }

            //todo: make null result descriptive
            var inviteRequest = await UnitOfWork.GetDbSet<AccommodationInvite>()
                .Where(invite => invite.Id == inviteId)
                .Include(invite => invite.Project)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            if (inviteRequest == null)
                throw new Exception("Invite request not found.");

            inviteRequest.IsAccepted = newState;
            inviteRequest.ResolveDescription = newState == AccommodationRequest.InviteState.Canceled
                ? ResolveDescription.Canceled
                : ResolveDescription.Declined;
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

            var receivers = await UnitOfWork
                .GetDbSet<Claim>()
                .Where(claim =>
                    claim.ClaimId == inviteRequest.FromClaimId ||
                    claim.ClaimId == inviteRequest.ToClaimId)
                .Include(c => c.Player)
                .ToArrayAsync()
                .ConfigureAwait(false);

            await EmailService
                .Email(await CreateInviteEmail<DeclineInviteEmail>(receivers,
                    inviteRequest.Project).ConfigureAwait(false))
                .ConfigureAwait(false);

            return inviteRequest;
        }

        public async Task DeclineAllClaimInvites(int claimId)
        {
            var inviteRequests = await UnitOfWork.GetDbSet<AccommodationInvite>()
                .Where(invite => invite.ToClaimId == claimId || invite.FromClaimId == claimId)
                .ToListAsync()
                .ConfigureAwait(false);

            if (inviteRequests.Count == 0)
                return;

            var claims = new List<int>();
            foreach (var accommodationInvite in inviteRequests)
            {
                claims.Add(accommodationInvite.FromClaimId);
                claims.Add(accommodationInvite.ToClaimId);
                accommodationInvite.IsAccepted = AccommodationRequest.InviteState.Declined;
                accommodationInvite.ResolveDescription = ResolveDescription.ClaimCanceled;
            }

            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

            claims = claims.Distinct().ToList();
            claims.Remove(claimId);

            var receivers = await UnitOfWork
                .GetDbSet<Claim>()
                .Where(claim => claims.Contains(claim.ClaimId))
                .Include(c => c.Player)
                .ToArrayAsync()
                .ConfigureAwait(false);

            var firstClaim = receivers.First();
            var project = await UnitOfWork.GetDbSet<Project>()
                .Where(proj => proj.ProjectId == firstClaim.ProjectId)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            await EmailService
                .Email(await CreateInviteEmail<DeclineInviteEmail>(receivers,
                    project).ConfigureAwait(false))
                .ConfigureAwait(false);
        }


        private async Task<AccommodationRequest> GetAccommodationRequestByClaim(int claimId) =>
            await UnitOfWork.GetDbSet<AccommodationRequest>()
                .Where(request => request.Subjects.Any(subject => subject.ClaimId == claimId))
                .Where(request => request.IsAccepted == AccommodationRequest.InviteState.Accepted)
                .Include(request => request.Subjects)
                .Include(request => request.AccommodationType)
                .Include(request => request.Accommodation)
                .FirstOrDefaultAsync().ConfigureAwait(false);


        private async Task DeclineOtherInvite(int claimId,
            int inviteId)
        {
            var inviteRequests = await UnitOfWork.GetDbSet<AccommodationInvite>()
                .Where(invite => invite.ToClaimId == claimId)
                .Where(invite => invite.Id != inviteId)
                .ToListAsync().ConfigureAwait(false);
            var stateToDecline = new[] {AccommodationRequest.InviteState.Unanswered};
            foreach (var accommodationInvite in inviteRequests)
            {
                if (!stateToDecline.Contains(accommodationInvite.IsAccepted))
                {
                    continue;
                }

                accommodationInvite.IsAccepted = AccommodationRequest.InviteState.Declined;
                accommodationInvite.ResolveDescription = ResolveDescription.DeclinedWithAcceptOther;
            }

            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
