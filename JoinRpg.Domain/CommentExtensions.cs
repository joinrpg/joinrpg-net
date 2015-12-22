using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static  class CommentExtensions
  {
    public static bool IsReadByUser(this Comment comment, int userId)
    {
      return comment.AuthorUserId == userId || (comment.Claim.Watermarks.SingleOrDefault(wm => wm.UserId == userId)?.CommentId  ?? 0) >= comment.CommentId;
    }
  }
}
