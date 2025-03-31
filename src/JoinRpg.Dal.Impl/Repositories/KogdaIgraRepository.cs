using System.Data.Entity;
using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.DataModel.Projects;

namespace JoinRpg.Dal.Impl.Repositories;
internal class KogdaIgraRepository(MyDbContext ctx) : RepositoryImplBase(ctx), IKogdaIgraRepository
{
    async Task<(int KogdaIgraId, string Name)[]> IKogdaIgraRepository.GetAll()
    {
        var result = await Ctx.Set<KogdaIgraGame>().Select(ki => new { ki.KogdaIgraGameId, ki.Name }).ToListAsync();
        return [.. result.Select(x => (x.KogdaIgraGameId, x.Name))];
    }

    async Task<KogdaIgraGame> IKogdaIgraRepository.GetById(int kogdaIgraId)
    {
        return await Ctx.Set<KogdaIgraGame>().FindAsync(kogdaIgraId);
    }
}
