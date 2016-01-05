using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static  class CommentExtensions
  {
    public static bool IsReadByUser(this Comment comment, int userId)
    {
      return comment.AuthorUserId == userId 
        || (comment.Claim.Watermarks.OrderByDescending(wm => wm.CommentId).FirstOrDefault(wm => wm.UserId == userId)?.CommentId  ?? 0) >= comment.CommentId;
    }
  }
}
