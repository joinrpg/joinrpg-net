using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Forums;

namespace JoinRpg.Data.Interfaces;

public interface IForumRepository
{
    Task<ForumThread> GetThread(ForumThreadIdentification forumThreadId);

    Task<CommentDiscussion?> GetDiscussionByComment(int projectId, int commentId);

    /// <summary>
    /// Темы форума для списка. Счётчики комментариев посчитаны в запросе для <paramref name="currentUserId"/>
    /// с учётом <paramref name="isMaster"/>: скрытые от игроков комментарии видны только мастеру.
    /// </summary>
    Task<IReadOnlyCollection<IForumThreadListItem>> GetThreads(int projectId, UserIdentification currentUserId, bool isMaster, IEnumerable<int>? groupIds);
    Task<ForumThreadHeader> GetThreadHeader(ForumThreadIdentification threadId);
}

public record class ForumThreadHeader(string Header, CharacterGroupIdentification CharacterGroupId);

public interface IForumThreadListItem
{
    ForumThreadIdentification Id { get; }
    string Header { get; }
    bool IsVisibleToPlayer { get; }
    int CharacterGroupId { get; }
    UserInfoHeader Topicstarter { get; }
    MarkdownDbValue LastMessageText { get; }
    UserInfoHeader? LastMessageAuthor { get; }
    DateTime UpdatedAt { get; }
    UserInfoHeader? LastMessageAuthorForPlayer { get; }
    MarkdownDbValue LastMessageTextForPlayer { get; }

    /// <summary>Непрочитанные текущим пользователем комментарии, которые ему видны.</summary>
    int UnreadCount { get; }

    /// <summary>Видимые текущему пользователю комментарии.</summary>
    int TotalCount { get; }
}
