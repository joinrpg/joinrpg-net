using JoinRpg.DataModel.Extensions;
using JoinRpg.DomainTypes.Characters.Claims;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

internal class UnifiedGridRepository(MyDbContext Ctx) : IUnifiedGridRepository
{
    public async Task<IReadOnlyCollection<UgDto>> GetByGroups(ProjectIdentification projectId, UgStatusSpec spec, IReadOnlyCollection<CharacterGroupIdentification> groups)
    {
        var groupPredicate = CharacterPredicates.ByGroup(groups);
        var result = await GetCoreAsync(projectId, spec, groupPredicate);
        return [.. result.Select(r => ToUgDto(r))];
    }

    public async Task<IReadOnlyCollection<UgDto>> GetAll(ProjectIdentification projectId, UgStatusSpec spec)
    {
        Expression<Func<Character, bool>> groupPredicate = character => true;
        var result = await GetCoreAsync(projectId, spec, groupPredicate);
        return [.. result.Select(r => ToUgDto(r))];
    }

    private async Task<IReadOnlyCollection<CoreResult>> GetCoreAsync(ProjectIdentification projectId, UgStatusSpec spec, Expression<Func<Character, bool>> groupPredicate)
    {
        var charStatusPredicate = CharacterPredicates.ByUgStatus(spec);
        var claimStatusPredicate = ClaimPredicates.ByUgStatus(spec);
        var activeClaimsPredicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);

        var query =
            from character in Ctx.Set<Character>().AsExpandable()
            where character.ProjectId == projectId.Value
            where groupPredicate.Invoke(character)
            where charStatusPredicate.Invoke(character)
            select
            new CoreResult
            {
                character = character,
                ApprovedUser = (int?)character.ApprovedClaim!.PlayerUserId,
                Claims = character
                    .Claims
                    .Where(c => claimStatusPredicate.Invoke(c))
                    .Select(
                     c => new ClaimResult
                     {
                         Claim = c,
                         FeePaid = (int?)c.FinanceOperations.Where(c => c.State == FinanceOperationState.Approved).Sum(c => c.MoneyAmount)
                     }),
                HasActiveClaims = character.Claims.Any(c => activeClaimsPredicate.Invoke(c)),
            };

        return await query.ToListAsync();
    }

    private static UgDto ToUgDto(CoreResult r)
    {
        return new UgDto(
            r.character.ToCharacterTypeInfo(),
            r.character.CharacterName,
            UserIdentification.FromOptional(r.ApprovedUser),
            r.character.IsActive,
            r.HasActiveClaims,
            r.character.GetId(),
            [.. r.Claims.Select(c => new UgClaim(c.Claim, c.FeePaid ?? 0))]);
    }

    private sealed class CoreResult
    {
        public required Character character { get; init; }
        public int? ApprovedUser { get; init; }
        public required IEnumerable<ClaimResult> Claims { get; init; }
        public bool HasActiveClaims { get; init; }
    }

    private sealed class ClaimResult
    {
        public required Claim Claim { get; init; }
        public int? FeePaid { get; init; }
    }
}
