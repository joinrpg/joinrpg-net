using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

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
                .Include(x => x.ProjectAccommodations).ToListAsync().ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ProjectAccommodationType>> GetPlayerSelectableAccommodationForProject(int projectId)
        {
            return await Ctx.Set<ProjectAccommodationType>().Where(a => a.ProjectId == projectId && a.IsPlayerSelectable == true)
                .Include(x => x.ProjectAccommodations).ToListAsync().ConfigureAwait(false);
        }
    }
}
