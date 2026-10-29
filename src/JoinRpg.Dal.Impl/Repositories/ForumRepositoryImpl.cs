using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Forums;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

internal class ForumRepositoryImpl(MyDbContext ctx) : GameRepositoryImplBase(ctx), IForumRepository
{
    public Task<ForumThread> GetThread(ForumThreadIdentification forumThreadId)
    {
        return
          Ctx.Set<ForumThread>()
            .Include(thread => thread.CommentDiscussion.Comments.Select(comment => comment.CommentText))
            .Include(thread => thread.CommentDiscussion.Comments.Select(comment => comment.Author))
            .Include(thread => thread.Project)
            .SingleOrDefaultAsync(thread => thread.ProjectId == forumThreadId.ProjectId && thread.ForumThreadId == forumThreadId.ThreadId);
    }

    public async Task<ForumThreadHeader> GetThreadHeader(ForumThreadIdentification threadId)
        => await GetThreadHeaderOrDefault(threadId)
            ?? throw new InvalidOperationException($"Forum thread {threadId} not found");

    public async Task<ForumThreadHeader?> GetThreadHeaderOrDefault(ForumThreadIdentification threadId)
    {
        int projectId = threadId.ProjectId;
        var forumThreadId = threadId.ThreadId;
        var result = await
         Ctx.Set<ForumThread>()
           .Where(thread => thread.ProjectId == projectId && thread.ForumThreadId == forumThreadId)
           .Select(ft =>
           new
           {
               ft.Header,
               ft.CharacterGroupId,
               ft.CommentDiscussionId,
               ft.IsVisibleToPlayer,
           }
           )
           .SingleOrDefaultAsync();

        return result is null
            ? null
            : new ForumThreadHeader(
                threadId,
                result.Header,
                new CharacterGroupIdentification(threadId.ProjectId, result.CharacterGroupId),
                new CommentDiscussionId(result.CommentDiscussionId),
                result.IsVisibleToPlayer);
    }

    public Task<CommentDiscussion?> GetDiscussionByComment(int projectId, int commentId)
    {
        return
          Ctx.Set<CommentDiscussion>()
            .Include(thread => thread.Comments.Select(comment => comment.CommentText))
            .Include(thread => thread.Comments.Select(comment => comment.Author))
            .Include(thread => thread.Project)
            .Where(discussion => discussion.Comments.Any(comment => comment.CommentId == commentId))
            .SingleOrDefaultAsync(thread => thread.ProjectId == projectId);
    }

    public async Task<IReadOnlyCollection<IForumThreadListItem>> GetThreads(int projectId, UserIdentification currentUserId, bool isMaster, IEnumerable<int>? groupIds)
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

        var userId = currentUserId.Value;
        var unreadCount = CommentDiscussionPredicates.UnreadCount;
        var visibleCount = CommentDiscussionPredicates.VisibleCount;

        var rows = await Ctx.Set<ForumThread>()
          .AsNoTracking()
          .AsExpandable()
          .Where(thread => thread.ProjectId == projectId)
          .Where(groupPredicate)
          .Where(thread => isMaster || thread.IsVisibleToPlayer)
          .Select(thread => new ForumThreadListRow()
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
              UnreadCount = unreadCount.Invoke(thread.CommentDiscussion, userId, isMaster),
              TotalCount = visibleCount.Invoke(thread.CommentDiscussion, isMaster),
              ForumThreadId = thread.ForumThreadId,
              CharacterGroupId = thread.CharacterGroupId,
              IsVisibleToPlayer = thread.IsVisibleToPlayer,
          })
          .ToListAsync();

        return [.. rows.Select(row => row.ToListItem())];
    }
}

/// <summary>
/// Строка выборки списка тем. EF-сущности пользователей живут только здесь и не выходят из репозитория.
/// </summary>
internal sealed class ForumThreadListRow
{
    public required int ProjectId { get; init; }
    public required int ForumThreadId { get; init; }
    public required string Header { get; init; }
    public required User Topicstarter { get; init; }
    public required MarkdownDbValue LastMessageText { get; init; }
    public required User? LastMessageAuthor { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required bool IsVisibleToPlayer { get; init; }
    public required int CharacterGroupId { get; init; }
    public required User? LastMessageAuthorForPlayer { get; init; }
    public required MarkdownDbValue LastMessageTextForPlayer { get; init; }
    public required int UnreadCount { get; init; }
    public required int TotalCount { get; init; }

    public IForumThreadListItem ToListItem() => new ForumThreadListItem(
        new ForumThreadIdentification(new ProjectIdentification(ProjectId), ForumThreadId),
        Header,
        IsVisibleToPlayer,
        CharacterGroupId,
        Topicstarter.ToUserInfoHeader(),
        LastMessageText,
        LastMessageAuthor?.ToUserInfoHeader(),
        UpdatedAt,
        LastMessageAuthorForPlayer?.ToUserInfoHeader(),
        LastMessageTextForPlayer,
        UnreadCount,
        TotalCount);
}

internal sealed record class ForumThreadListItem(
    ForumThreadIdentification Id,
    string Header,
    bool IsVisibleToPlayer,
    int CharacterGroupId,
    UserInfoHeader Topicstarter,
    MarkdownDbValue LastMessageText,
    UserInfoHeader? LastMessageAuthor,
    DateTime UpdatedAt,
    UserInfoHeader? LastMessageAuthorForPlayer,
    MarkdownDbValue LastMessageTextForPlayer,
    int UnreadCount,
    int TotalCount) : IForumThreadListItem;
