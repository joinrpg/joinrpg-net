using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories
{
    public class AccommodationRequestRepositoryImpl : RepositoryImplBase,
        IAccommodationRequestRepository
    {
        public AccommodationRequestRepositoryImpl(MyDbContext ctx) : base(ctx)
        {
        }

        public async Task<IReadOnlyCollection<AccommodationRequest>>
            GetAccommodationRequestForProject(int projectId)
        {
            return await Ctx.Set<AccommodationRequest>()
                .Where(request => request.ProjectId == projectId)
                .ToListAsync().ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<AccommodationRequest>>
            GetAccommodationRequestForClaim(int claimId)
        {
            return await Ctx.Set<AccommodationRequest>().Where(request =>
                    request.Subjects.Any(subject => subject.ClaimId == claimId))
                .Include(request => request.Subjects)
                .Include(request => request.Subjects.Select(cl => cl.Player))
                .ToListAsync().ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<Claim>>
            GetClaimsWithSameAccommodationType(int accommodationTypeId)
        {
            return await Ctx.Set<AccommodationRequest>().Where(request =>
                    request.AccommodationTypeId == accommodationTypeId)
                .SelectMany(request => request.Subjects)
                .Where(claim => claim.ClaimStatus == Claim.Status.Approved)
                .Include(claim => claim.Player)
                .ToListAsync().ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<Claim>>
            GetClaimsWithSameAccommodationTypeToInvite(int accommodationTypeId)
        {
            return await Ctx.Set<AccommodationRequest>().Where(request =>
                    (request.AccommodationTypeId == accommodationTypeId &&
                     request.AccommodationId == null))
                .SelectMany(request => request.Subjects)
                .Where(claim => claim.ClaimStatus == Claim.Status.Approved)
                .Include(claim => claim.Player)
                .ToListAsync().ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<Claim>> GetClaimsWithSameAccommodationRequest(
            int accommodationRequestId)
        {
            var tmp = await Ctx.Set<AccommodationRequest>()
                .Where(request => request.Id == accommodationRequestId)
                .SelectMany(request => request.Subjects)
                .Where(claim => claim.ClaimStatus == Claim.Status.Approved)
                .Include(claim => claim.Player)
                .ToListAsync().ConfigureAwait(false);
            return tmp;
        }

        public async Task<IEnumerable<Claim>> GetClaimsWithOutAccommodationRequest(
            int projectId)
        {
            var projectClaims = await Ctx.Set<Claim>()
                .Where(claim => claim.ProjectId == projectId)
                .Include(claim => claim.Player)
                .Where(claim => claim.ClaimStatus == Claim.Status.Approved)
                .ToListAsync().ConfigureAwait(false);
            var claimsWithAccommodationRequest = await Ctx.Set<AccommodationRequest>()
                .Where(request => request.ProjectId == projectId)
                .SelectMany(request => request.Subjects)
                .Where(claim => claim.ClaimStatus == Claim.Status.Approved)
                .Include(claim => claim.Player)
                .ToListAsync().ConfigureAwait(false);
            return
                projectClaims.Except(claimsWithAccommodationRequest);
        }
    }

}
