using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
    [UsedImplicitly]
    public class AccommodationInviteServiceImpl: DbServiceImplBase, IAccommodationInviteService
    {
        private const string AutomaticDeclineByAcceptOther = "Приглашение отклонено автоматически, из-за принятия другого приглашения";
        private const string AutomaticDeclineByAcceptOtherToGroup = "Приглашение отклонено автоматически, из-за принятия другого группового приглашения";
        private const string ManualAccept = "Приглашение принято";
        private const string ManualDecline = "Приглашение отклонено";
        private const string ManualCancel = "Приглашение было отозвано";


        public AccommodationInviteServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public async Task<AccommodationInvite> CreateAccommodationInvite(int projectId,
           int senderClaimId,
           int receiverClaimId,
           int accommodationRequestId,
           bool inviteWithGroup = false)
        {
            //todo: make null result descriptive

            var receiverCurrentAccommodationRequest = await UnitOfWork.GetDbSet<AccommodationRequest>()
                .Where(request => request.Subjects.Any(subject => subject.ClaimId == receiverClaimId))
                .Where(request => request.IsAccepted == AccommodationRequest.InviteState.Accepted)
                .Include(request => request.Subjects)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            var senderAccommodationRequest = await UnitOfWork.GetDbSet<AccommodationRequest>()
                .Where(request => request.Id == accommodationRequestId)
                .Include(request => request.Subjects)
                .Include(request => request.AccommodationType)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            if (receiverCurrentAccommodationRequest?.AccommodationId != null)
            {
                return null;
            }

            var canInvite = (inviteWithGroup && senderAccommodationRequest.Subjects.Count +
                             receiverCurrentAccommodationRequest?.Subjects.Count >
                             senderAccommodationRequest.AccommodationType.Capacity) ||
                            (!inviteWithGroup && senderAccommodationRequest.Subjects.Count <
                             senderAccommodationRequest.AccommodationType.Capacity);

            if (!canInvite)
            {
                return null;
            }

            var inviteRequest = new AccommodationInvite
            {
                ProjectId = projectId,
                FromClaimId = senderClaimId,
                ToClaimId = receiverClaimId,
                IsAccepted = AccommodationRequest.InviteState.Unanswered
            };

            UnitOfWork.GetDbSet<AccommodationInvite>().Add(inviteRequest);
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

            return inviteRequest;
        }


        public async Task<AccommodationInvite> AcceptAccommodationInvite(int projectId,
            int inviteId,
            bool inviteWithGroup = false)
        {
            //todo: make null result descriptive
            var inviteRequest = await UnitOfWork.GetDbSet<AccommodationInvite>()
                .Where(invite => invite.Id == inviteId)
                .Include(invite => invite.To)
                .Include(invite => invite.From)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            var receiverAccommodationRequest = await GetAccommodationRequestByClaim(inviteRequest.ToClaimId).ConfigureAwait(false);
            var senderAccommodationRequest = await GetAccommodationRequestByClaim(inviteRequest.FromClaimId).ConfigureAwait(false);

            if (receiverAccommodationRequest?.AccommodationId != null)
            {
                return null;
            }

            var canInvite = (inviteWithGroup && senderAccommodationRequest.Subjects.Count +
                             receiverAccommodationRequest?.Subjects.Count >
                             senderAccommodationRequest.AccommodationType.Capacity) ||
                            (!inviteWithGroup && senderAccommodationRequest.Subjects.Count <
                             senderAccommodationRequest.AccommodationType.Capacity);

            if (!canInvite)
            {
                return null;
            }

            receiverAccommodationRequest?.Subjects.Remove(inviteRequest.To);

            await DeclineOtherInvite(inviteRequest.ToClaimId, inviteId).ConfigureAwait(false);


            if (inviteWithGroup)
            {
                foreach (var claim in receiverAccommodationRequest?.Subjects)
                {
                    await DeclineOtherInvite(claim.ClaimId, inviteId, onlyGroupInvite: true).ConfigureAwait(false);
                }
            }

            inviteRequest.IsAccepted = AccommodationRequest.InviteState.Accepted;
            inviteRequest.ResolveDescription = ManualAccept;
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

            return inviteRequest;
        }

        public async Task<AccommodationInvite> CancelOrDeclineAccommodationInvite(int inviteId, AccommodationRequest.InviteState newState)
        {
            var acceptedStates = new[]
            {
                AccommodationRequest.InviteState.Declined, AccommodationRequest.InviteState.Canceled
            };

            if (!acceptedStates.Contains(newState))
            {
                return null;
            }

            //todo: make null result descriptive
            var inviteRequest = await UnitOfWork.GetDbSet<AccommodationInvite>()
                .Where(invite => invite.Id == inviteId)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            inviteRequest.IsAccepted = newState;
            inviteRequest.ResolveDescription = newState == AccommodationRequest.InviteState.Canceled ? ManualCancel : ManualDecline;
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

            return inviteRequest;
        }


        private async Task<AccommodationRequest> GetAccommodationRequestByClaim(int claimId) => await UnitOfWork.GetDbSet<AccommodationRequest>()
            .Where(request => request.Subjects.Any(subject => subject.ClaimId == claimId))
            .Where(request => request.IsAccepted == AccommodationRequest.InviteState.Accepted)
            .Include(request => request.Subjects)
            .FirstOrDefaultAsync().ConfigureAwait(false);


        private async Task DeclineOtherInvite(int claimId, int inviteId, bool onlyGroupInvite = false)
        {
            var inviteRequests = await UnitOfWork.GetDbSet<AccommodationInvite>()
                .Where(invite => invite.ToClaimId == claimId)
                .Where(invite => invite.Id != inviteId)
                .ToListAsync().ConfigureAwait(false);
            foreach (var accommodationInvite in inviteRequests)
            {

                if (!(onlyGroupInvite && accommodationInvite.IsGroupInvite || !onlyGroupInvite))
                {
                    continue;
                }

                accommodationInvite.IsAccepted = AccommodationRequest.InviteState.Declined;
                accommodationInvite.ResolveDescription = onlyGroupInvite ? AutomaticDeclineByAcceptOtherToGroup : AutomaticDeclineByAcceptOther;
            }
            await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
