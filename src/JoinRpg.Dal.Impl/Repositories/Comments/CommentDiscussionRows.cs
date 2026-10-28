using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Dal.Impl.Repositories.Comments;

/// <summary>
/// Поля владельца обсуждения в плоской проекции: обсуждение принадлежит либо заявке, либо теме
/// форума, связь хранится на их стороне.
/// </summary>
internal interface ICommentDiscussionOwnerRow
{
    int CommentDiscussionId { get; }
    int ProjectId { get; }
    int? ClaimId { get; }

    /// <summary>
    /// Проект заявки. Хранится отдельно от проекта обсуждения: адрес владельца строится от него, а
    /// расхождение с проектом обсуждения — рассогласованные данные.
    /// </summary>
    int? ClaimProjectId { get; }
    int? ForumThreadId { get; }

    /// <summary>Проект темы. См. <see cref="ClaimProjectId"/>.</summary>
    int? ForumThreadProjectId { get; }
    int? ForumThreadCharacterGroupId { get; }
    bool? ForumThreadIsVisibleToPlayer { get; }
}

/// <summary>
/// Владелец обсуждения без комментариев — проекция для <c>GetOwner*</c>.
/// </summary>
/// <remarks>
/// Обычный класс с <c>init</c>-свойствами, а не позиционный record: EF6 не умеет конструировать
/// record'ы в проекции (ADR013 §5).
/// </remarks>
internal sealed class CommentDiscussionOwnerRow : ICommentDiscussionOwnerRow
{
    public required int CommentDiscussionId { get; init; }
    public required int ProjectId { get; init; }
    public required int? ClaimId { get; init; }
    public required int? ClaimProjectId { get; init; }
    public required int? ForumThreadId { get; init; }
    public required int? ForumThreadProjectId { get; init; }
    public required int? ForumThreadCharacterGroupId { get; init; }
    public required bool? ForumThreadIsVisibleToPlayer { get; init; }
}

/// <summary>
/// Обсуждение целиком — проекция для <c>GetDiscussion</c>. См. замечания к
/// <see cref="CommentDiscussionOwnerRow"/>; <see cref="Comments"/> объявлена как
/// <see cref="IEnumerable{T}"/>, а не коллекция, по той же причине.
/// </summary>
internal sealed class CommentDiscussionRow : ICommentDiscussionOwnerRow
{
    public required int CommentDiscussionId { get; init; }
    public required int ProjectId { get; init; }
    public required int? ClaimId { get; init; }
    public required int? ClaimProjectId { get; init; }
    public required int? ForumThreadId { get; init; }
    public required int? ForumThreadProjectId { get; init; }
    public required int? ForumThreadCharacterGroupId { get; init; }
    public required bool? ForumThreadIsVisibleToPlayer { get; init; }
    public required IEnumerable<CommentRow> Comments { get; init; }
}

/// <summary>
/// Комментарий в плоской проекции. Финоперация разложена на nullable-скаляры: её может не быть,
/// признак наличия — <see cref="FinanceOperationType"/>.
/// </summary>
internal sealed class CommentRow
{
    public required int CommentId { get; init; }
    public required int? ParentCommentId { get; init; }
    public required int AuthorUserId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsCommentByPlayer { get; init; }
    public required bool IsVisibleToPlayer { get; init; }
    public required CommentExtraAction? ExtraAction { get; init; }
    public required string? Text { get; init; }
    public required int? FinanceMoneyAmount { get; init; }
    public required FinanceOperationType? FinanceOperationType { get; init; }
    public required FinanceOperationState? FinanceState { get; init; }
    public required DateTime? FinanceOperationDate { get; init; }
    public required int? FinancePaymentTypeId { get; init; }
}
