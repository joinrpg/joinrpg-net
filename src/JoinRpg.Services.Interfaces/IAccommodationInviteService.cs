using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
    public interface IAccommodationInviteService
    {/*
        Task<AccommodationInvite> CreateAccommodationInvite(int projectId,
            int senderClaimId,
            int receiverClaimId,
            int accommodationRequestId);
            */
        Task<IEnumerable<AccommodationInvite?>?> CreateAccommodationInviteToGroupOrClaim(int projectId,
            int senderClaimId,
            string receiverClaimOrAccommodationRequestId,
            int accommodationRequestId,
            string accommodationRequestPrefix);

        Task<AccommodationInvite?> CancelOrDeclineAccommodationInvite(int inviteId,
            AccommodationRequest.InviteState newState);

        Task<AccommodationInvite?> AcceptAccommodationInvite(int projectId,
            int inviteId);

        Task DeclineAllClaimInvites(int claimId);
    }
}
