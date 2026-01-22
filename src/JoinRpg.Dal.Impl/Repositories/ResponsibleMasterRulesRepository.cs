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

    public async Task<List<CharacterGroup>> GetResponsibleMasterRulesForMaster(ProjectIdentification projectId, UserIdentification userId)
    {
        return
            await Ctx.Set<CharacterGroup>()
            .Where(cg => cg.ProjectId == projectId && cg.ResponsibleMasterUserId != userId.Value)
            .ToListAsync();
    }
}
