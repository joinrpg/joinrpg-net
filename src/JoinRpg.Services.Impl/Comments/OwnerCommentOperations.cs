using JoinRpg.DomainTypes.Forums;

namespace JoinRpg.Services.Impl.Comments;

/// <summary>
/// Комментарий к заявке — операция агрегата персонажа (ADR014) со всеми побочными эффектами
/// заявки. Снаружи доступна только через <see cref="ICommentDiscussionService"/>, который
/// проверяет общие правила обсуждения (ADR026 §5).
/// </summary>
internal interface IClaimCommentOperation
{
    Task AddComment(ClaimIdentification claimId, int? parentCommentId, bool isVisibleToPlayer, string commentText);
}

/// <summary>
/// Комментарий к теме форума. Снаружи доступна только через <see cref="ICommentDiscussionService"/>,
/// который проверяет общие правила обсуждения (ADR026 §5).
/// </summary>
internal interface IForumCommentOperation
{
    Task AddComment(ForumThreadIdentification forumThreadId, int? parentCommentId, bool isVisibleToPlayer, string commentText);
}
