using System.Data.Entity;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Dal.Impl.Repositories;
internal class ResponsibleMasterRulesRepository(MyDbContext ctx) : RepositoryImplBase(ctx), IResponsibleMasterRulesRepository
{
    public async Task<List<CharacterGroup>> GetResponsibleMasterRules(ProjectIdentification projectId)
    {
        return
            await Ctx.Set<CharacterGroup>()
            .Where(cg => cg.ProjectId == projectId && cg.ResponsibleMasterUserId != null)
            .ToListAsync();
    }
}
