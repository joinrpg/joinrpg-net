using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

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

    /// <summary>
    /// Правила переноса заявки поверх доменного агрегата.
    /// </summary>
    /// <remarks>
    /// Раньше это был extension-метод на EF-сущности в продакшн-коде. После перевода
    /// <c>ClaimServiceImpl</c> на <c>CharacterInfo</c> у него не осталось вызывающих, кроме тестов,
    /// поэтому он живёт здесь — собирать агрегат из мока и звать общий валидатор.
    /// </remarks>
    public static IReadOnlyCollection<ClaimForbiddenReason> ValidateIfCanMoveClaim(
        this Character claimSource, MockedProject mock, Claim claim, UserInfo userInfo, ProjectInfo projectInfo)
        => ClaimValidator.Validate(
            mock.GetCharacterInfo(claimSource),
            userInfo,
            new UserClaimInfo(claim.GetId(), claim.ClaimStatus),
            projectInfo,
            ClaimOperation.MoveByMaster);
}
