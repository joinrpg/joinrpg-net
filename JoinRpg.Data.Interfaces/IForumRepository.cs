using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
  public interface IForumRepository
  {
    Task<ForumThread> GetThread(int projectId, int? forumThreadId);
    Task<CommentDiscussion> GetDiscussion(int projectId, int commentDiscussionId);

    Task<CommentDiscussion> GetDiscussionByComment(int projectId, int commentId);

    Task<IReadOnlyCollection<IForumThreadListItem>> GetThreads(int projectId, bool isMaster, IEnumerable<CharacterGroup> groupIds);
  }

  public interface IForumThreadListItem : ICommentDiscussionHeader
  {
    string Header { get; }
    User Topicstarter { get; }
    MarkdownString LastMessageText { get; }
    User LastMessageAuthor { get; }
    DateTime UpdatedAt { get; }
  }
}
