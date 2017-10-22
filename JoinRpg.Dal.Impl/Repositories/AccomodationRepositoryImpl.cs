﻿using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories
{
    public class AccomodationRepositoryImpl : RepositoryImplBase, IAccomodationRepository
    {
        public AccomodationRepositoryImpl(MyDbContext ctx) : base(ctx)
        {
        }

        public async Task<IReadOnlyCollection<ProjectAccomodationType>> GetAccomodationForProject(int projectId)
        {
            return await Ctx.Set<ProjectAccomodationType>().Where(a => a.ProjectId == projectId).Include(x=>x.ProjectAccomodations).ToListAsync();
        }
    }
}
