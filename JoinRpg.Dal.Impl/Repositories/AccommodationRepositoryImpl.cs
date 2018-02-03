using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories
{
    public class AccommodationRepositoryImpl : RepositoryImplBase, IAccommodationRepository
    {
        public AccommodationRepositoryImpl(MyDbContext ctx) : base(ctx)
        {
        }

        public async Task<IReadOnlyCollection<ProjectAccommodationType>> GetAccommodationForProject(
            int projectId)
        {
            return await Ctx.Set<ProjectAccommodationType>().Where(a => a.ProjectId == projectId)
                .Include(x => x.ProjectAccommodations)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ProjectAccommodationType>> GetPlayerSelectableAccommodationForProject(int projectId)
        {
            return await Ctx.Set<ProjectAccommodationType>().Where(a => a.ProjectId == projectId && a.IsPlayerSelectable)
                .Include(x => x.ProjectAccommodations).ToListAsync().ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ClaimAccommodationInfoRow>>
            GetClaimAccommodationReport(int project)
        {

            return await Ctx.Set<Claim>().AsExpandable().Include(claim => claim.Player.Extra)
                .Where(ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active))
                .Where(claim => claim.ProjectId == project)
                .Select(
                    claim => new ClaimAccommodationInfoRow()
                    {
                        ClaimId = claim.ClaimId,
                        AccomodationType = claim.AccommodationRequest != null
                            ? claim.AccommodationRequest.AccommodationType.Name
                            : null,
                        RoomName =
                            claim.AccommodationRequest != null &&
                            claim.AccommodationRequest.Accommodation != null
                                ? claim.AccommodationRequest.Accommodation.Name
                                : null,
                        User = claim.Player,
                    }).ToListAsync();

        }
    }
}
