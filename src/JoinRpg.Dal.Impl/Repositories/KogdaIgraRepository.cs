using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.DataModel.Projects;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Dal.Impl.Repositories;

internal class KogdaIgraRepository(MyDbContext Ctx) : IKogdaIgraRepository
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
           .OrderByDescending(e => e.Projects.Count)
           .ThenByDescending(e => e.UpdateRequestedAt)
           ;
    }

    private IQueryable<KogdaIgraGame> GetSet()
    {
        return Ctx.Set<KogdaIgraGame>()
                   .Include(ki => ki.Projects);
    }

    private static Expression<Func<KogdaIgraGame, bool>> UpdateRequestedPredicate() => e => e.LastUpdatedAt == null || e.LastUpdatedAt < e.UpdateRequestedAt;

    async Task<(KogdaIgraIdentification KogdaIgraId, string Name)[]> IKogdaIgraRepository.GetActive()
    {
        var result = await GetSet()
            .AsNoTracking()
            .Where(ki => ki.Active)
            .Where(ki => ki.LastUpdatedAt!.Value.Year > (DateTimeOffset.Now.Year - 2))
            .Where(ki => ki.Name.Length > 0)
            .Select(ki => new { ki.KogdaIgraGameId, ki.Name })
            .ToListAsync();
        return [.. result.Select(x => (new KogdaIgraIdentification(x.KogdaIgraGameId), x.Name))];
    }

    public Task<int> GetNotUpdatedCount() => GetNotUpdatedQuery().CountAsync();

    async Task<ICollection<KogdaIgraGame>> IKogdaIgraRepository.GetByIds(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIdentifications)
    {
        var ids = kogdaIgraIdentifications.Select(x => x.Value).ToArray();
        return await Ctx.Set<KogdaIgraGame>().Where(ki => ids.Contains(ki.KogdaIgraGameId)).ToListAsync();
    }

    async Task<IReadOnlyCollection<KogdaIgraGameData>> IKogdaIgraRepository.GetDataByIds(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIdentifications)
    {
        var ids = kogdaIgraIdentifications.Select(x => x.Value).ToArray();
        var list = await Ctx.Set<KogdaIgraGame>().Where(ki => ids.Contains(ki.KogdaIgraGameId)).ToListAsync();
        return [.. list.Select(TryConvert).WhereNotNull()];
    }

    internal static KogdaIgraGameData? TryConvert(KogdaIgraGame g)
    {
        if (g.LastUpdatedAt is null || g.Begin is null || g.End is null)
        {
            return null;
        }
        return new KogdaIgraGameData(
                    new(g.KogdaIgraGameId),
                    g.Name,
                    DateOnly.FromDateTime(g.Begin.Value.Date),
                    DateOnly.FromDateTime(g.End.Value.Date),
                    g.RegionName,
                    g.MasterGroupName,
                    Uri.TryCreate(g.SiteUri, UriKind.Absolute, out var u) ? u : null,
                    g.Active);
    }
}
