using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.DomainTypes.Characters.UnifiedGrid;

/// <summary>
/// Статус в объединённом гриде заявок (ADR027): какие персонажи попадают в грид и какие их заявки
/// показываются под ними.
/// </summary>
public enum UnifiedGridStatus
{
    /// <summary>Живые персонажи и их активные заявки.</summary>
    Active,

    /// <summary>Живые персонажи без принятой заявки, кроме NPC, и их активные заявки.</summary>
    Vacant,

    /// <summary>Живые персонажи без принятой заявки, но с активными заявками.</summary>
    Discussion,

    /// <summary>Живые персонажи с принятой заявкой; в строке показывается только она.</summary>
    Approved,

    /// <summary>Живые персонажи с заявками в листе ожидания; в строке показываются только они.</summary>
    OnHold,

    /// <summary>
    /// Удалённые персонажи (и без заявок тоже) со всеми заявками и живые персонажи с отклонёнными
    /// или отозванными заявками — только с ними.
    /// </summary>
    Archive,

    /// <summary>Все персонажи, включая удалённых, и все их заявки.</summary>
    All,
}

public static class UnifiedGridStatusExtensions
{
    /// <summary>
    /// Каких персонажей просить у репозитория: архиву и «всем» нужны и удалённые (архиву заодно и
    /// живые — ради их отклонённых заявок), остальным статусам — только живые. Точный отбор внутри
    /// набора делает <see cref="Matches"/>, но тащить агрегат на заведомо ненужных персонажей
    /// незачем.
    /// </summary>
    public static CharacterStatusSpec GetCharacterStatusSpec(this UnifiedGridStatus status)
        => status is UnifiedGridStatus.Archive or UnifiedGridStatus.All
            ? CharacterStatusSpec.Any
            : CharacterStatusSpec.Active;

    /// <summary>
    /// Подходит ли персонаж под статус.
    /// </summary>
    public static bool Matches(this UnifiedGridStatus status, CharacterInfo character)
        => status switch
        {
            UnifiedGridStatus.Active => character.IsActive,
            UnifiedGridStatus.Vacant => character.IsActive
                && character.ApprovedClaimId is null
                && character.CharacterType != CharacterType.NonPlayer,
            UnifiedGridStatus.Discussion => character.IsActive
                && character.ApprovedClaimId is null
                && character.HasActiveClaims,
            UnifiedGridStatus.Approved => character.IsActive && character.ApprovedClaimId is not null,
            UnifiedGridStatus.OnHold => character.IsActive && character.Claims.Any(IsOnHold),
            UnifiedGridStatus.Archive => !character.IsActive || character.Claims.Any(IsDeclined),
            UnifiedGridStatus.All => true,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    /// <summary>
    /// Какие заявки персонажа показывать под ним.
    /// </summary>
    /// <remarks>
    /// Лист ожидания у живого персонажа — не архив, хотя заявка там и неактивна
    /// (<see cref="ClaimStatusExtensions.IsActive"/>). У удалённого персонажа активных заявок не бывает
    /// (удаление их не допускает), а оставшиеся — в том числе из листа ожидания — показываем все.
    /// </remarks>
    public static IReadOnlyCollection<CharacterClaimInfo> SelectClaims(this UnifiedGridStatus status, CharacterInfo character)
        => [.. status switch
        {
            UnifiedGridStatus.All => character.Claims,
            UnifiedGridStatus.Archive when !character.IsActive => character.Claims,
            UnifiedGridStatus.Archive => character.Claims.Where(IsDeclined),
            UnifiedGridStatus.Approved => character.Claims.Where(claim => claim.IsApproved),
            UnifiedGridStatus.OnHold => character.Claims.Where(IsOnHold),
            _ => character.Claims.Where(claim => claim.IsActive),
        }];

    private static bool IsDeclined(CharacterClaimInfo claim)
        => claim.Status is ClaimStatus.DeclinedByMaster or ClaimStatus.DeclinedByUser;

    private static bool IsOnHold(CharacterClaimInfo claim) => claim.Status == ClaimStatus.OnHold;
}
