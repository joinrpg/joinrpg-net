using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain;

/// <summary>
/// Правила заявки поверх EF-сущности <see cref="Character"/>.
/// </summary>
/// <remarks>
/// Сами правила живут в <c>JoinRpg.DomainTypes</c> (<see cref="ClaimValidator"/>). Здесь остался
/// только адаптер <see cref="LegacyClaimTarget"/> для тех мест, которые ещё не умеют работать с
/// <c>CharacterInfo</c>: <c>AddClaimViewModel</c> (упирается в <c>CustomFieldsViewModel</c>) и
/// <c>CharacterListViewService</c>. Удалить вместе с ними — тогда правила везде будут принимать
/// агрегат напрямую (ADR013).
/// </remarks>
public static class ClaimAcceptOrMoveValidationExtensions
{
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
    {
        ArgumentNullException.ThrowIfNull(claimSource);

#pragma warning disable CS0618 // Пока эти вызывающие живут на EF-сущности, см. LegacyClaimTarget
        return ClaimValidator.Validate(
            new LegacyClaimTarget(claimSource), userInfo, movedClaim: null, projectInfo, operation);
#pragma warning restore CS0618
    }
}
