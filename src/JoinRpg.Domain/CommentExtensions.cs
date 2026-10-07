namespace JoinRpg.Domain;

public static class CommentExtensions
{
    public static bool IsReadByUser(this Comment comment, int userId) => comment.Discussion.GetWatermark(userId) >= comment.CommentId;

    private static int GetWatermark(this ICommentDiscussionHeader discussion, int userId)
    {
        var comments = discussion.Comments.Where(c => c.AuthorUserId == userId).Select(c => c.Id);
        var watermarks = discussion.Watermarks.Where(wm => wm.UserId == userId).Select(c => c.CommentId);
        return comments.Union(watermarks).Append(0).Max();
    }

    public static int GetUnreadCount(this ICommentDiscussionHeader commentDiscussion, int currentUserId, ProjectInfo projectInfo)
    {
        var watermark = commentDiscussion.GetWatermark(currentUserId);
        return commentDiscussion.Comments.Where(c => c.IsVisibleTo(currentUserId, projectInfo)).Count(comment => watermark < comment.Id);
    }

    /// <summary>
    /// Мастерский доступ берётся из <paramref name="projectInfo"/>, а не из <c>comment.Project.ProjectAcls</c>:
    /// навигация стоила ленивой догрузки ACL на страницах форума и обсуждений (#4989).
    /// </summary>
    public static bool IsVisibleTo(this ICommentHeader comment, int currentUserId, ProjectInfo projectInfo)
        => comment.IsVisibleToPlayer || projectInfo.HasMasterAccess(new UserIdentification(currentUserId));
}
