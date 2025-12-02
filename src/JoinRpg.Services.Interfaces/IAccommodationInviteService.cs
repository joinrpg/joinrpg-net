using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces;

public interface IAccommodationInviteService
{/*
    Task<AccommodationInvite> CreateAccommodationInvite(int projectId,
        int senderClaimId,
        int receiverClaimId,
        int accommodationRequestId);
        */
    Task<IEnumerable<AccommodationInvite?>?> CreateAccommodationInviteToGroupOrClaim(int projectId,
        int senderClaimId,
        int receiverClaimOrAccommodationRequestId,
        int accommodationRequestId);

    Task<AccommodationInvite?> CancelOrDeclineAccommodationInvite(int inviteId,
        AccommodationRequest.InviteState newState);

    Task<AccommodationInvite?> AcceptAccommodationInvite(int projectId,
        int inviteId);

    Task DeclineAllClaimInvites(ClaimIdentification claimId);
}
