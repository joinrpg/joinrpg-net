using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.DomainTypes.Comments;

/// <summary>
/// Доступ игрока к обсуждению по его владельцу (ADR026 §1) — без EF и без перебора проекта.
/// </summary>
/// <remarks>
/// Мастерский доступ здесь не проверяется: он одинаков для любого владельца и берётся из
/// <c>ProjectInfo.HasMasterAccess</c>. Совпадение проекта владельца с проектом маршрута — тоже
/// забота вызывающего (404).
/// </remarks>
public static class CommentDiscussionAccess
{
    /// <summary>
    /// Обсуждение заявки видит её игрок — в любом статусе заявки, как и саму заявку.
    /// </summary>
    /// <param name="owner">Владелец обсуждения.</param>
    /// <param name="claim">Заявка владельца.</param>
    /// <param name="userId">Текущий пользователь.</param>
    /// <exception cref="ArgumentException">Передана не та заявка, что владеет обсуждением.</exception>
    public static bool HasPlayerAccess(this ClaimDiscussionOwner owner, ClaimInCharacter claim, UserIdentification userId)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(claim);

        if (claim.ClaimId != owner.ClaimId)
        {
            throw new ArgumentException($"Claim {claim.ClaimId} does not own discussion of claim {owner.ClaimId}", nameof(claim));
        }

        return claim.Claim.PlayerId == userId;
    }

    /// <summary>
    /// Тему форума видит игрок, если она видна игрокам и у него есть принятая заявка на персонажа
    /// из группы темы (с учётом вложенности групп).
    /// </summary>
    /// <param name="owner">Владелец обсуждения.</param>
    /// <param name="characters">
    /// Персонажи, среди которых искать принятые заявки игрока. Лишние персонажи не мешают: принятость
    /// заявки и её игрок проверяются здесь, а не полагаются на выборку вызывающего.
    /// </param>
    /// <param name="userId">Текущий пользователь.</param>
    public static bool HasPlayerAccess(this ForumDiscussionOwner owner, IEnumerable<CharacterInfo> characters, UserIdentification userId)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(characters);

        return owner.IsVisibleToPlayer
            && characters.Any(character =>
                character.ApprovedClaim?.PlayerId == userId
                && character.ParentGroupIdsToTop.Contains(owner.CharacterGroupId));
    }
}
