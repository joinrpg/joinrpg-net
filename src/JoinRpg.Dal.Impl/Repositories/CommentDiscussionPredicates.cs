namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Правила по обсуждению, которые считаются в SQL: списки тем форума и заявок (ADR026, §2).
/// </summary>
internal static class CommentDiscussionPredicates
{
    /// <summary>
    /// Число непрочитанных пользователем комментариев.
    /// </summary>
    /// <remarks>
    /// Комментарий непрочитан, если он виден пользователю и лежит выше отметки прочтения — максимума
    /// из его собственных комментариев и его отметок в обсуждении (то же правило — у отметки в
    /// <c>DiscussionViewModelBuilder</c>).
    /// «Выше максимума из двух» записано как «выше каждого из них»: <c>Math.Max</c> EF6 не транслирует.
    /// Применять через <c>Invoke</c> на запросе с <c>AsExpandable()</c>.
    /// </remarks>
    public static Expression<Func<CommentDiscussion, int, bool, int>> UnreadCount { get; } =
        (discussion, userId, isMaster) => discussion.Comments.Count(comment =>
            (comment.IsVisibleToPlayer || isMaster)
            && comment.CommentId > (discussion.Comments.Where(own => own.AuthorUserId == userId).Max(own => (int?)own.CommentId) ?? 0)
            && comment.CommentId > (discussion.Watermarks.Where(watermark => watermark.UserId == userId).Max(watermark => (int?)watermark.CommentId) ?? 0));

    /// <summary>
    /// Число видимых пользователю комментариев.
    /// </summary>
    public static Expression<Func<CommentDiscussion, bool, int>> VisibleCount { get; } =
        (discussion, isMaster) => discussion.Comments.Count(comment => comment.IsVisibleToPlayer || isMaster);
}
