using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
    public interface IAccommodationRequestRepository
    {
        Task<IReadOnlyCollection<AccommodationRequest>> GetAccommodationRequestForProject(int projectId);
        Task<IReadOnlyCollection<AccommodationRequest>> GetAccommodationRequestForClaim(int claimId);
        Task<IReadOnlyCollection<Claim>> GetClaimsWithSameAccommodationType(int accommodationTypeId);
        Task<IReadOnlyCollection<Claim>> GetClaimsWithSameAccommodationTypeToInvite(int accommodationTypeId);
        Task<IReadOnlyCollection<Claim>> GetClaimsWithSameAccommodationRequest(int accommodationRequestId);
        Task<IEnumerable<Claim>> GetClaimsWithOutAccommodationRequest(int projectId);
    }

}
