using System.Data.Entity;
using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.DataModel.Projects;

namespace JoinRpg.Dal.Impl.Repositories;
internal class KogdaIgraRepository(MyDbContext ctx) : RepositoryImplBase(ctx), IKogdaIgraRepository
{
    public async Task<(int KogdaIgraId, string Name)[]> GetNotUpdated()
    {
        var result = await GetNotUpdatedQuery().Take(100).Select(ki => new { ki.KogdaIgraGameId, ki.Name }).ToListAsync();
        return [.. result.Select(x => (x.KogdaIgraGameId, x.Name))];
    }

    public Task<KogdaIgraGame[]> GetNotUpdatedObjects() => GetNotUpdatedQuery().Take(100).ToArrayAsync();
    private IQueryable<KogdaIgraGame> GetNotUpdatedQuery()
    {
        return GetSet()
           .Where(UpdateRequestedPredicate())
           .OrderByDescending(e => e.UpdateRequestedAt)
           ;
    }

    private IQueryable<KogdaIgraGame> GetSet()
    {
        return Ctx.Set<KogdaIgraGame>()
                   .Include(ki => ki.Projects);
    }

    private static System.Linq.Expressions.Expression<Func<KogdaIgraGame, bool>> UpdateRequestedPredicate() => e => e.LastUpdatedAt == null || e.LastUpdatedAt < e.UpdateRequestedAt;

    async Task<(int KogdaIgraId, string Name)[]> IKogdaIgraRepository.GetActive()
    {
        var result = await GetSet().Where(ki => ki.Active).Select(ki => new { ki.KogdaIgraGameId, ki.Name }).ToListAsync();
        return [.. result.Select(x => (x.KogdaIgraGameId, x.Name))];
    }

    async Task<KogdaIgraGame> IKogdaIgraRepository.GetById(int kogdaIgraId)
    {
        return await Ctx.Set<KogdaIgraGame>().FindAsync(kogdaIgraId);
    }

    public Task<int> GetNotUpdatedCount() => GetNotUpdatedQuery().CountAsync();
}
