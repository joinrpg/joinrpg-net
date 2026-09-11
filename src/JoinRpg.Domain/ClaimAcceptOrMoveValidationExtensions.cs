using System.Diagnostics.CodeAnalysis;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain;

/// <summary>
/// Правила заявки поверх EF-сущности <see cref="Character"/>: тонкая обёртка над
/// <see cref="ClaimValidator"/>.
/// </summary>
/// <remarks>
/// Сами правила и трансляция причин в исключения живут в <c>JoinRpg.DomainTypes</c>
/// (<see cref="ClaimValidator"/>). Здесь остаётся только то, что без EF не работает: адаптер
/// <see cref="LegacyClaimTarget"/> и преобразование EF-<see cref="Claim"/> в <see cref="UserClaimInfo"/>.
/// </remarks>
public static class ClaimAcceptOrMoveValidationExtensions
{
    public static bool IsAcceptingClaims(this Character character, ProjectInfo projectInfo)
        => !ValidateIfCanAddClaim(character, userInfo: null, projectInfo, ClaimOperation.DisplayForPlayer).Any();

    /// <summary>
    /// Причины, по которым нельзя создать заявку на этого персонажа.
    /// </summary>
    /// <param name="userInfo">
    /// Игрок, на которого оформляется заявка; <c>null</c> — если он неизвестен (тогда правила про
    /// контакты и уже поданные заявки не считаются).
    /// </param>
    /// <param name="operation">
    /// Для <see cref="ClaimOperation.DisplayForPlayer"/> мастерских послаблений нет: страница
    /// показывает положение дел глазами игрока, кто бы её ни открыл.
    /// </param>
    public static IReadOnlyCollection<ClaimForbiddenReason> ValidateIfCanAddClaim(
        this Character claimSource,
        UserInfo? userInfo, ProjectInfo projectInfo, ClaimOperation operation)
        => Validate(claimSource, userInfo, movedClaim: null, projectInfo, operation);

    public static IReadOnlyCollection<ClaimForbiddenReason> ValidateIfCanMoveClaim(this Character claimSource, Claim claim, UserInfo userInfo, ProjectInfo projectInfo)
        => Validate(claimSource, userInfo, ToMovedClaim(claim), projectInfo, ClaimOperation.MoveByMaster);

    /// <param name="userInfo">
    /// Игрок, на которого оформляется заявка. При <see cref="ClaimOperation.AddByMaster"/> это не
    /// тот, кто выполняет операцию: правила про контакты и уже поданные заявки относятся к игроку.
    /// </param>
    public static void EnsureCanAddClaim([NotNull] this Character claimSource, UserInfo userInfo, ProjectInfo projectInfo, ClaimOperation operation)
    {
        ArgumentNullException.ThrowIfNull(claimSource);
        ThrowIfValidationFailed(
            claimSource.ValidateIfCanAddClaim(userInfo, projectInfo, operation),
            claim: null,
            projectInfo);
    }

    public static void EnsureCanMoveClaim([NotNull] this Character claimSource, Claim claim, UserInfo userInfo, ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(claimSource);
        ThrowIfValidationFailed(claimSource.ValidateIfCanMoveClaim(claim, userInfo, projectInfo), ToMovedClaim(claim), projectInfo);
    }

#pragma warning disable CS0618 // Пока часть вызывающего кода живёт на EF-сущности, см. LegacyClaimTarget
    private static IReadOnlyCollection<ClaimForbiddenReason> Validate(
        Character character, UserInfo? userInfo, UserClaimInfo? movedClaim, ProjectInfo projectInfo, ClaimOperation operation)
        => ClaimValidator.Validate(new LegacyClaimTarget(character), userInfo, movedClaim, projectInfo, operation);
#pragma warning restore CS0618

    private static UserClaimInfo ToMovedClaim(Claim claim) => new(claim.GetId(), claim.ClaimStatus);

    private static void ThrowIfValidationFailed(
        IReadOnlyCollection<ClaimForbiddenReason> validation,
        UserClaimInfo? claim,
        ProjectInfo projectInfo)
    {
        if (validation.Count > 0)
        {
            ClaimValidator.ThrowForReason(validation.First(), claim, projectInfo);
        }
    }
}
