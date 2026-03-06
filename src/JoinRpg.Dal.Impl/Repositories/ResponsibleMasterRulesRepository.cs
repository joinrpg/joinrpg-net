namespace JoinRpg.Dal.Impl.Repositories;

internal class ResponsibleMasterRulesRepository(MyDbContext ctx) : IResponsibleMasterRulesRepository
{
    public async Task<List<CharacterGroup>> GetResponsibleMasterRules(ProjectIdentification projectId)
    {
        return
            await ctx.Set<CharacterGroup>()
            .Where(cg => cg.ProjectId == projectId && cg.ResponsibleMasterUserId != null)
            .ToListAsync();
    }

    public async Task<List<CharacterGroup>> GetResponsibleMasterRulesForMaster(ProjectIdentification projectId, UserIdentification userId)
    {
        return
            await ctx.Set<CharacterGroup>()
            .Where(cg => cg.ProjectId == projectId && cg.ResponsibleMasterUserId != userId.Value)
            .ToListAsync();
    }
}
