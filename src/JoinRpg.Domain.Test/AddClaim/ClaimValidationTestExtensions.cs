using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.AddClaim;

internal static class ClaimValidationTestExtensions
{
    /// <summary>
    /// Причины запрета без флагов — чтобы тесты про правила говорили только о правилах,
    /// а свойства причин проверялись отдельно (<see cref="ClaimForbiddenReasonTableTest"/>).
    /// </summary>
    public static IReadOnlyCollection<AddClaimForbideReason> Kinds(
        this IReadOnlyCollection<ClaimForbiddenReason> reasons)
        => [.. reasons.Select(r => r.Kind)];
}
