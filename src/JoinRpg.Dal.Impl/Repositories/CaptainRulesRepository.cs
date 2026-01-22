using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.Dal.Impl.Repositories;

internal class CaptainRulesRepository(MyDbContext ctx) : ICaptainRulesRepository
{
    async Task<IReadOnlyCollection<CaptainAccessRule>> ICaptainRulesRepository.GetCaptainRules(ProjectIdentification projectIdentification)
        => await GetImpl(projectIdentification, x => x.ProjectId == projectIdentification.Value);
    async Task<IReadOnlyCollection<CaptainAccessRule>> ICaptainRulesRepository.GetCaptainRules(ProjectIdentification projectIdentification, UserIdentification userId)
        => await GetImpl(projectIdentification, x => x.ProjectId == projectIdentification.Value && x.CaptainUserId == userId.Value);

    private async Task<IReadOnlyCollection<CaptainAccessRule>> GetImpl(
        ProjectIdentification projectIdentification,
        Expression<Func<CaptainAccessRuleEntity, bool>> predicate)
    {
        var result = await ctx.Set<CaptainAccessRuleEntity>()
                    .Where(predicate).ToListAsync();
        return [.. result.Select(x => new CaptainAccessRule(new CharacterGroupIdentification(projectIdentification, x.CharacterGroupId), new(x.CaptainUserId), x.CanApprove))];
    }

}
