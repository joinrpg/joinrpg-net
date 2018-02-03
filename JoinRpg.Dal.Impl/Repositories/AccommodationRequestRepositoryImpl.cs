using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories
{
    public class AccommodationRequestRepositoryImpl : RepositoryImplBase,IAccommodationRequestRepository
    {
        public AccommodationRequestRepositoryImpl(MyDbContext ctx) : base(ctx)
        {
        }

        public async Task<IReadOnlyCollection<AccommodationRequest>> GetAccommodationRequestForProject(int projectId)
        {
            return await Ctx.Set<AccommodationRequest>().Where(request => request.ProjectId == projectId)
                .ToListAsync().ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<AccommodationRequest>> GetAccommodationRequestForClaim(int claimId)
        {
            return await Ctx.Set<AccommodationRequest>().Where(request => request.Subjects.Any(subject => subject.ClaimId == claimId))
                .ToListAsync().ConfigureAwait(false);
        }
    }
}
