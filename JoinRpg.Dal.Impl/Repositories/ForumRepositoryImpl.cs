using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories
{
  [UsedImplicitly(ImplicitUseTargetFlags.Itself)]
  public class ForumRepositoryImpl : GameRepositoryImplBase, IForumRepository
  {
    public ForumRepositoryImpl(MyDbContext ctx) : base(ctx)
    {
    }

    public Task<ForumThread> GetThread(int projectId, int? forumThreadId)
    {
      return
        Ctx.Set<ForumThread>()
          .Include(thread => thread.CommentDiscussion.Comments.Select(comment => comment.CommentText))
          .Include(thread => thread.CommentDiscussion.Comments.Select(comment => comment.Author))
          .Include(thread => thread.Project)
          .SingleOrDefaultAsync(thread => thread.ProjectId == projectId && thread.ForumThreadId == forumThreadId);
    }

    public Task<CommentDiscussion> GetDiscussion(int projectId, int commentDiscussionId)
    {
      return
        Ctx.Set<CommentDiscussion>()
          .Include(thread => thread.Comments.Select(comment => comment.CommentText))
          .Include(thread => thread.Comments.Select(comment => comment.Author))
          .Include(thread => thread.Project)
          .SingleOrDefaultAsync(thread => thread.ProjectId == projectId && thread.CommentDiscussionId == commentDiscussionId);
    }

    public Task<CommentDiscussion> GetDiscussionByComment(int projectId, int commentId)
    {
      return
        Ctx.Set<CommentDiscussion>()
          .Include(thread => thread.Comments.Select(comment => comment.CommentText))
          .Include(thread => thread.Comments.Select(comment => comment.Author))
          .Include(thread => thread.Project)
          .Where(discussion => discussion.Comments.Any(comment => comment.CommentId == commentId))
          .SingleOrDefaultAsync(thread => thread.ProjectId == projectId);
    }
  }
}
