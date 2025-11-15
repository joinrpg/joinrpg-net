using JoinRpg.PrimitiveTypes.Claims;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;
internal class UnifiedGridRepository(MyDbContext Ctx) : IUnifiedGridRepository
{
    public async Task<IReadOnlyCollection<UgDto>> GetByGroups(ProjectIdentification projectId, UgStatusSpec spec, IReadOnlyCollection<CharacterGroupIdentification> groups)
    {
        var groupPredicate = CharacterPredicates.ByGroupImprecise(groups);
        var charStatusPredicate = CharacterPredicates.ByUgStatus(spec);
        var claimStatusPredicate = ClaimPredicates.ByUgStatus(spec);

        var activeClaimsPredicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);

        var query =
            from character in Ctx.Set<Character>().AsExpandable()
            where groupPredicate.Invoke(character)
            where charStatusPredicate.Invoke(character)
            select
            new
            {
                character,
                ApprovedUser = (int?)character.ApprovedClaim!.PlayerUserId,
                Claims = character
                    .Claims
                    .Where(c => claimStatusPredicate.Invoke(c))
                    .Select(
                     c => new
                     {
                         Claim = c,
                         c.Player,
                         c.LastMasterCommentBy,
                         c.LastVisibleMasterCommentBy,
                         c.FinanceOperations,
                         FeePaid = (int?)c.FinanceOperations.Where(c => c.State == FinanceOperationState.Approved).Sum(c => c.MoneyAmount)
                     }),
                HasActiveClaims = character.Claims.Any(c => activeClaimsPredicate.Invoke(c)),
            };

        var result = await query.ToListAsync();
        var groupPrecise = CharacterPredicates.ByGroupPrecise(groups);

        return [.. result
            .Where(x => groupPrecise(x.character))
            .Select(r =>
        new UgDto(r.character.CharacterName,
                  UserIdentification.FromOptional(r.ApprovedUser),
                  r.character.IsPublic,
                  r.character.CharacterType,
                  r.HasActiveClaims,
                  r.character.CharacterSlotLimit,
                  r.character.GetId(),
                  [..r.Claims.Select(c => new UgClaim(c.Claim, c.FeePaid ?? 0))])
        )];

    }
}
