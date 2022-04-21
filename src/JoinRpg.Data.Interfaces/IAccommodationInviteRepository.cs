using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces;

public interface IAccommodationInviteRepository
{
    Task<IEnumerable<AccommodationInvite>> GetIncomingInviteForClaim(Claim claim);
    Task<IEnumerable<AccommodationInvite>> GetIncomingInviteForClaim(int claimId);

    Task<IEnumerable<AccommodationInvite>> GetOutgoingInviteForClaim(Claim claim);
    Task<IEnumerable<AccommodationInvite>> GetOutgoingInviteForClaim(int claimId);

}
