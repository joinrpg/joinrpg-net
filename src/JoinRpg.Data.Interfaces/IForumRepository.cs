using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.Forums;

namespace JoinRpg.Data.Interfaces;

public interface IForumRepository
{
    Task<ForumThread> GetThread(ForumThreadIdentification forumThreadId);
    Task<CommentDiscussion> GetDiscussion(int projectId, int commentDiscussionId);

    Task<CommentDiscussion> GetDiscussionByComment(int projectId, int commentId);

    Task<IReadOnlyCollection<IForumThreadListItem>> GetThreads(int projectId, bool isMaster, IEnumerable<int>? groupIds);
    Task<ForumThreadHeader> GetThreadHeader(ForumThreadIdentification threadId);
}

public record class ForumThreadHeader(string Header, CharacterGroupIdentification CharacterGroupId);

public interface IForumThreadListItem : ICommentDiscussionHeader, IForumThread
{
    string Header { get; }
    User Topicstarter { get; }
    MarkdownString LastMessageText { get; }
    User LastMessageAuthor { get; }
    DateTime UpdatedAt { get; }
    User LastMessageAuthorForPlayer { get; set; }
    MarkdownString LastMessageTextForPlayer { get; set; }
}
