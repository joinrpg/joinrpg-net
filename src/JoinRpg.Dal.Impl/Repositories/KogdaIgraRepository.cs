using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.DataModel.Projects;

namespace JoinRpg.Dal.Impl.Repositories;
internal class KogdaIgraRepository(MyDbContext ctx) : RepositoryImplBase(ctx), IKogdaIgraRepository
{
    public async Task<(KogdaIgraIdentification KogdaIgraId, string Name)[]> GetNotUpdated()
    {
        var result = await GetNotUpdatedQuery().Take(100).Select(ki => new { ki.KogdaIgraGameId, ki.Name }).ToListAsync();
        return [.. result.Select(x => (new KogdaIgraIdentification(x.KogdaIgraGameId), x.Name))];
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

    async Task<(KogdaIgraIdentification KogdaIgraId, string Name)[]> IKogdaIgraRepository.GetActive()
    {
        var result = await GetSet()
            .Where(ki => ki.Active)
            .Where(ki => ki.LastUpdatedAt!.Value.Year > (DateTimeOffset.Now.Year - 2))
            .Where(ki => ki.Name.Length > 0)
            .Select(ki => new { ki.KogdaIgraGameId, ki.Name })
            .ToListAsync();
        return [.. result.Select(x => (new KogdaIgraIdentification(x.KogdaIgraGameId), x.Name))];
    }

    async Task<KogdaIgraGame> IKogdaIgraRepository.GetById(int kogdaIgraId)
    {
        return await Ctx.Set<KogdaIgraGame>().FindAsync(kogdaIgraId);
    }

    public Task<int> GetNotUpdatedCount() => GetNotUpdatedQuery().CountAsync();

    async Task<ICollection<KogdaIgraGame>> IKogdaIgraRepository.GetByIds(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIdentifications)
    {
        var ids = kogdaIgraIdentifications.Select(x => x.Value).ToArray();
        return await Ctx.Set<KogdaIgraGame>().Where(ki => ids.Contains(ki.KogdaIgraGameId)).ToListAsync();
    }
}
