using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
    public interface IAccommodationInviteService
    {
        Task<AccommodationInvite> CreateAccommodationInvite(int projectId,
            int senderClaimId,
            int receiverClaimId,
            int accommodationRequestId,
            bool inviteWithGroup = false);

        Task<AccommodationInvite> CancelOrDeclineAccommodationInvite(int inviteId,
            AccommodationRequest.InviteState newState);

        Task<AccommodationInvite> AcceptAccommodationInvite(int projectId,
            int inviteId);
    }
}
