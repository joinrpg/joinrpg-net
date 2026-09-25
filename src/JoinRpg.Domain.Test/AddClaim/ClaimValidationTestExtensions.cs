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
    /// Правила подачи заявки поверх доменного агрегата.
    /// </summary>
    /// <remarks>
    /// Как и <see cref="ValidateIfCanMoveClaim"/>, раньше жил в продакшн-коде extension-методом
    /// на EF-сущности (поверх <c>LegacyClaimTarget</c>). После переезда страницы подачи заявки на
    /// <c>CharacterInfo</c> вызывающих, кроме тестов, не осталось.
    /// </remarks>
    /// <param name="projectInfo">
    /// Может отличаться от того, к которому привязан агрегат: тесты подменяют статус проекта
    /// через <c>WithChangedStatus</c>, а правила берут проект отдельным параметром.
    /// </param>
    public static IReadOnlyCollection<ClaimForbiddenReason> ValidateIfCanAddClaim(
        this Character claimSource, MockedProject mock, UserInfo? userInfo, ProjectInfo projectInfo, ClaimOperation operation)
        => ClaimValidator.Validate(
            mock.GetCharacterInfo(claimSource), userInfo, movedClaim: null, projectInfo, operation);

    /// <inheritdoc cref="ValidateIfCanAddClaim" />
    public static bool IsAvailableForPlayer(
        this Character claimSource, MockedProject mock, ProjectInfo projectInfo)
        => ClaimValidator.IsAvailableForPlayer(mock.GetCharacterInfo(claimSource), projectInfo);

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
