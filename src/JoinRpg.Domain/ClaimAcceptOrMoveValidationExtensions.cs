using System.Diagnostics.CodeAnalysis;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain;

/// <summary>
/// Правила заявки поверх EF-сущности <see cref="Character"/>: тонкая обёртка над
/// <see cref="ClaimValidator"/> плюс трансляция причин в исключения.
/// </summary>
/// <remarks>
/// Сами правила живут в <c>JoinRpg.DomainTypes</c>. Здесь остаётся только то, что без EF не
/// работает: адаптер <see cref="LegacyClaimTarget"/> и <see cref="ThrowForReason"/>, которому для
/// <see cref="ClaimWrongStatusException"/> нужна сущность заявки.
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
        ThrowIfValidationFailed(claimSource.ValidateIfCanMoveClaim(claim, userInfo, projectInfo), claim, projectInfo);
    }

#pragma warning disable CS0618 // Пока часть вызывающего кода живёт на EF-сущности, см. LegacyClaimTarget
    private static IReadOnlyCollection<ClaimForbiddenReason> Validate(
        Character character, UserInfo? userInfo, UserClaimInfo? movedClaim, ProjectInfo projectInfo, ClaimOperation operation)
        => ClaimValidator.Validate(new LegacyClaimTarget(character), userInfo, movedClaim, projectInfo, operation);
#pragma warning restore CS0618

    private static UserClaimInfo ToMovedClaim(Claim claim) => new(claim.GetId(), claim.ClaimStatus);

    private static void ThrowIfValidationFailed(
        IReadOnlyCollection<ClaimForbiddenReason> validation,
        Claim? claim,
        ProjectInfo projectInfo)
    {
        if (validation.Count > 0)
        {
            ThrowForReason(validation.First(), claim, projectInfo);
        }
    }

    internal static void ThrowForReason(ClaimForbiddenReason reason, Claim? claim, ProjectInfo projectInfo)
    {
        throw reason.Kind switch
        {
            AddClaimForbideReason.ProjectNotActive => new ProjectDeactivatedException(projectInfo.ProjectId),

            AddClaimForbideReason.ProjectClaimsClosed or AddClaimForbideReason.SlotsExhausted
                or AddClaimForbideReason.Busy or AddClaimForbideReason.Npc or AddClaimForbideReason.CharacterInactive
                    => new ClaimTargetIsNotAcceptingClaims(),

            AddClaimForbideReason.AlreadySent => new ClaimAlreadyPresentException(),
            AddClaimForbideReason.OnlyOneCharacter => new OnlyOneApprovedClaimException(),

            AddClaimForbideReason.ApprovedClaimMovedToSlot or AddClaimForbideReason.CheckedInClaimCantBeMoved => new ClaimWrongStatusException(claim!.GetId(), claim.ClaimStatus),
            AddClaimForbideReason.RealNameMissing or AddClaimForbideReason.PhoneMissing or
            AddClaimForbideReason.TelegramMissing or AddClaimForbideReason.VkontakteMissing => new InsufficientContactsException(),
            _ => new ArgumentOutOfRangeException(nameof(reason), reason.Kind, message: null),
        };
    }
}
