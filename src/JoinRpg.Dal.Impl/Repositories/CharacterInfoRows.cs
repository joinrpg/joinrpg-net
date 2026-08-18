using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Плоская проекция строки персонажа для запроса <see cref="CharacterInfoRepository"/>.
/// </summary>
/// <remarks>
/// Обычный класс с <c>init</c>-свойствами, а не позиционный record: EF6 не умеет конструировать
/// record'ы в проекции. По той же причине <see cref="Claims"/> объявлена как
/// <see cref="IEnumerable{T}"/>, а не как коллекция.
/// </remarks>
internal sealed class CharacterInfoRow
{
    public required int CharacterId { get; init; }
    public required string CharacterName { get; init; }
    public required CharacterType CharacterType { get; init; }
    public required bool IsHot { get; init; }
    public required int? CharacterSlotLimit { get; init; }
    public required bool IsPublic { get; init; }
    public required bool HidePlayerForCharacter { get; init; }
    public required bool IsActive { get; init; }
    public required bool InGame { get; init; }
    public required bool AutoCreated { get; init; }
    public required string? JsonData { get; init; }
    public required MarkdownDbValue Description { get; init; }
    public required IntList ParentGroups { get; init; }
    public required int? ApprovedClaimId { get; init; }
    public required int? OriginalCharacterSlotId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required int CreatedById { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required int UpdatedById { get; init; }
    public required IEnumerable<CharacterInfoClaimRow> Claims { get; init; }
}

/// <summary>Плоская проекция строки заявки. См. замечания к <see cref="CharacterInfoRow"/>.</summary>
internal sealed class CharacterInfoClaimRow
{
    public required int ClaimId { get; init; }
    public required int PlayerUserId { get; init; }
    public required ClaimStatus ClaimStatus { get; init; }
    public required ClaimDenialReason? ClaimDenialStatus { get; init; }
    public required int ResponsibleMasterUserId { get; init; }
    public required DateTime CreateDate { get; init; }
    public required DateTime LastUpdateDateTime { get; init; }
    public required DateTime? CheckInDate { get; init; }
    public required DateTimeOffset? LastPlayerCommentAt { get; init; }
    public required DateTimeOffset? LastMasterCommentAt { get; init; }
    public required DateTimeOffset? LastVisibleMasterCommentAt { get; init; }
    public required int? CurrentFee { get; init; }
    public required bool PreferentialFeeUser { get; init; }
    public required string? JsonData { get; init; }

    /// <summary>
    /// Сумма подтверждённых финансовых операций. <c>null</c>, если операций нет — из-за каста
    /// <c>(int?)</c> перед <c>Sum</c>, без которого EF6 падает на пустой коллекции.
    /// </summary>
    public required int? FeePaid { get; init; }

    /// <summary>Стоимость выбранного проживания; <c>null</c>, если проживание не выбрано.</summary>
    public required int? AccommodationFee { get; init; }
}
