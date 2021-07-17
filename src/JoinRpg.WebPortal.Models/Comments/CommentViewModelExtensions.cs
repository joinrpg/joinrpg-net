using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
    public static class CommentViewModelExtensions
    {
        public static List<CommentViewModel> ToCommentTreeViewModel(this CommentDiscussion discussion, int currentUserId)
        {
            return discussion.Comments.Where(comment => comment.ParentCommentId == null)
              .Select(comment => new CommentViewModel(discussion, comment, currentUserId, 0)).OrderBy(c => c.CreatedTime).ToList();
        }
    }
}

