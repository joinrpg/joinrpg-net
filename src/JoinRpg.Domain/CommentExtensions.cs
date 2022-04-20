using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
    public static class CommentExtensions
    {
        public static bool IsReadByUser(this Comment comment, int userId) => comment.Discussion.GetWatermark(userId) >= comment.CommentId;

        private static int GetWatermark(this ICommentDiscussionHeader discussion, int userId)
        {
            var comments = discussion.Comments.Where(c => c.AuthorUserId == userId).Select(c => c.Id);
            var watermarks = discussion.Watermarks.Where(wm => wm.UserId == userId).Select(c => c.CommentId);
            return comments.Union(watermarks).Append(0).Max();
        }

        public static bool HasMasterCommentsInLastXDays(this Claim claim, int days) => claim.LastVisibleMasterCommentAt?.AddDays(days) >= DateTimeOffset.Now;

        public static int GetUnreadCount(this ICommentDiscussionHeader commentDiscussion, int currentUserId)
        {
            var watermark = commentDiscussion.GetWatermark(currentUserId);
            return commentDiscussion.Comments.Where(c => c.IsVisibleTo(currentUserId)).Count(comment => watermark < comment.Id);
        }

        public static bool IsVisibleTo(this ICommentHeader comment, int currentUserId) => comment.IsVisibleToPlayer || comment.Project.HasMasterAccess(currentUserId);
    }
}
