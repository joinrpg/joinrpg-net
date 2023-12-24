using System.Data.Entity;
using System.Linq.Expressions;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories;

internal class ForumRepositoryImpl(MyDbContext ctx) : GameRepositoryImplBase(ctx), IForumRepository
{
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

    public async Task<IReadOnlyCollection<IForumThreadListItem>> GetThreads(int projectId, bool isMaster, IEnumerable<int>? groupIds)
    {
        Expression<Func<ForumThread, bool>> groupPredicate;
        if (groupIds == null)
        {
            groupPredicate = thread => true;
        }
        else
        {
            groupPredicate = thread => groupIds.Contains(thread.CharacterGroupId);
        }

        return await Ctx.Set<ForumThread>()
          .Where(thread => thread.ProjectId == projectId)
          .Where(groupPredicate)
          .Where(thread => isMaster || thread.IsVisibleToPlayer)
          .Select(thread => new ForumThreadListImpl()
          {
              ProjectId = thread.ProjectId,
              Header = thread.Header,
              LastMessageAuthor =
              thread.CommentDiscussion.Comments.OrderByDescending(c => c.CommentId).FirstOrDefault()!.Author,
              LastMessageText =
              thread.CommentDiscussion.Comments.OrderByDescending(c => c.CommentId).FirstOrDefault()!.CommentText.Text,
              LastMessageAuthorForPlayer =
              thread.CommentDiscussion.Comments.OrderByDescending(c => c.CommentId).FirstOrDefault(c => c.IsVisibleToPlayer)!.Author,
              LastMessageTextForPlayer =
              thread.CommentDiscussion.Comments.OrderByDescending(c => c.CommentId).FirstOrDefault(c => c.IsVisibleToPlayer)!.CommentText.Text,
              Topicstarter = thread.AuthorUser,
              UpdatedAt = thread.ModifiedAt,
              Comments = thread.CommentDiscussion.Comments.Select(c => new CommentHeaderImpl
              {
                  ProjectId = c.ProjectId,
                  AuthorUserId = c.AuthorUserId,
                  Project = c.Project,
                  IsVisibleToPlayer = c.IsVisibleToPlayer,
                  Id = c.CommentId,
              }).ToList(),
              Watermarks = thread.CommentDiscussion.Watermarks.ToList(),
              Project = thread.Project,
              Id = thread.ForumThreadId,
              CharacterGroupId = thread.CharacterGroupId,
              IsVisibleToPlayer = thread.IsVisibleToPlayer,
          })
          .ToListAsync();
    }
}

public class CommentHeaderImpl : ICommentHeader
{
    public int Id { get; set; }
    public required Project Project { get; set; }
    public int ProjectId { get; set; }
    public int AuthorUserId { get; set; }
    public bool IsVisibleToPlayer { get; set; }
}

internal class ForumThreadListImpl : IForumThreadListItem
{
    public required int ProjectId { get; set; }
    public required string Header { get; set; }
    public required User Topicstarter { get; set; }
    public required MarkdownString LastMessageText { get; set; }
    public required User LastMessageAuthor { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required int Id { get; set; }
    public required Project Project { get; set; }
    IEnumerable<ReadCommentWatermark> ICommentDiscussionHeader.Watermarks => Watermarks;

    IEnumerable<ICommentHeader> ICommentDiscussionHeader.Comments => Comments;

    internal required List<CommentHeaderImpl> Comments { private get; set; }
    internal required List<ReadCommentWatermark> Watermarks { private get; set; }
    public required bool IsVisibleToPlayer { get; set; }
    public required int CharacterGroupId { get; set; }
    public required User LastMessageAuthorForPlayer { get; set; }
    public required MarkdownString LastMessageTextForPlayer { get; set; }
}
