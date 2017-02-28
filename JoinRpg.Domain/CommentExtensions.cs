using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
  public static class CommentExtensions
  {
    public static bool IsReadByUser(this Comment comment, int userId)
    {
      return comment.Discussion.GetWatermark(userId) >= comment.CommentId;
    }

    private static int GetWatermark(this ICommentDiscussionHeader discussion, int userId)
    {
      var comments = discussion.Comments.Where(c => c.AuthorUserId == userId).Select(c => c.Id);
      var watermarks = discussion.Watermarks.Where(wm => wm.UserId == userId).Select(c => c.CommentId);
      return comments.Union(watermarks).Union(0).Max();
    }

    public static IEnumerable<Comment> InLastXDays(this IEnumerable<Comment> masterAnswers, int days)
    {
      return masterAnswers.Where(comment => DateTime.UtcNow.Subtract(comment.CreatedAt) < TimeSpan.FromDays(days));
    }

    public static int GetUnreadCount(this ICommentDiscussionHeader commentDiscussion, int currentUserId)
    {
      var watermark = commentDiscussion.GetWatermark(currentUserId);
      return commentDiscussion.Comments.Where(c => c.IsVisibleTo(currentUserId)).Count(comment => watermark < comment.Id);
    }

    public static bool IsVisibleTo(this ICommentHeader comment, int currentUserId)
    {
      return comment.IsVisibleToPlayer || comment.Project.HasMasterAccess(currentUserId);
    }
  }
}
