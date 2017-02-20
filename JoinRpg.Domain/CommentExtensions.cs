using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static  class CommentExtensions
  {
    public static bool IsReadByUser(this Comment comment, int userId)
    {
      return IsReadByUserImpl(comment, userId, comment.Discussion.Comments, comment.Discussion.GetWatermark(userId));
    }

    public static int GetUnreadCount(this IReadOnlyCollection<ICommentHeader> comments, int userId, int watermarkId)
    {
      return comments.Count(comment => IsReadByUserImpl(comment, userId, comments, watermarkId));
    }

    private static bool IsReadByUserImpl(ICommentHeader comment, int userId, IEnumerable<ICommentHeader> allCommentsInDiscussion, int watermark)
    {
      return comment.AuthorUserId == userId
             || allCommentsInDiscussion.Where(c => c.AuthorUserId == userId).Any(c => c.Id > comment.Id)
             || watermark >= comment.Id;
    }

    private static int GetWatermark(this ICommentDiscussionHeader discussion, int userId)
    {
      return discussion.Watermarks.OrderByDescending(wm => wm.CommentId).FirstOrDefault(wm => wm.UserId == userId)?.CommentId  ?? 0;
    }

    public static IEnumerable<Comment> InLastXDays(this IEnumerable<Comment> masterAnswers, int days)
    {
      return masterAnswers.Where(comment => DateTime.UtcNow.Subtract(comment.CreatedAt) < TimeSpan.FromDays(days));
    }

    public static int GetUnreadCount(this ICommentDiscussionHeader commentDiscussion, int currentUserId)
    {
      var hasMasterAccess = commentDiscussion.HasMasterAccess(currentUserId);
      return commentDiscussion.Comments.Where(c => c.IsVisibleToPlayer || hasMasterAccess)
        .ToList().GetUnreadCount(currentUserId, commentDiscussion.GetWatermark(currentUserId));
    }
  }
}
