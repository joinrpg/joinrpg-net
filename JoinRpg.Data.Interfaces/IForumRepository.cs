using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
  public interface IForumRepository
  {
    Task<ForumThread> GetThread(int projectId, int? forumThreadId);
    Task<CommentDiscussion> GetDiscussion(int projectId, int commentDiscussionId);

    Task<CommentDiscussion> GetDiscussionByComment(int projectId, int commentId);
  }
}
