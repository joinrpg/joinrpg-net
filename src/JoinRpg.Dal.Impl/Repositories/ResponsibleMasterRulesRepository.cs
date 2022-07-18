using System.Data.Entity;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Dal.Impl.Repositories;
internal class ResponsibleMasterRulesRepository : RepositoryImplBase, IResponsibleMasterRulesRepository
{
    public ResponsibleMasterRulesRepository(MyDbContext ctx) : base(ctx)
    {
    }

    public async Task<List<CharacterGroup>> GetResponsibleMasterRules(ProjectIdentification projectId)
    {
        return
            await Ctx.Set<CharacterGroup>()
            .Where(cg => cg.ProjectId == projectId && cg.ResponsibleMasterUserId != null)
            .ToListAsync();
    }
}
