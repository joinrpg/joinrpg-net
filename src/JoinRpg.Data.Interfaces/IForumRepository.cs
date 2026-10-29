using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Comments;
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

    /// <summary>
    /// Заголовок темы без комментариев. <c>null</c>, если такой темы в проекте нет.
    /// </summary>
    Task<ForumThreadHeader?> GetThreadHeaderOrDefault(ForumThreadIdentification threadId);
}

/// <summary>Заголовок темы форума — всё, кроме обсуждения (ADR026, шаг 6).</summary>
/// <param name="ThreadId">Тема.</param>
/// <param name="Header">Название темы.</param>
/// <param name="CharacterGroupId">Группа, к которой относится тема.</param>
/// <param name="CommentDiscussionId">Обсуждение темы — грузится отдельно доменным снимком.</param>
/// <param name="IsVisibleToPlayer">Видна ли тема игрокам вообще.</param>
public record class ForumThreadHeader(
    ForumThreadIdentification ThreadId,
    string Header,
    CharacterGroupIdentification CharacterGroupId,
    CommentDiscussionId CommentDiscussionId,
    bool IsVisibleToPlayer)
{
    /// <summary>Владелец обсуждения темы — для проверки доступа без загрузки обсуждения.</summary>
    public ForumDiscussionOwner ToDiscussionOwner() => new(ThreadId, CharacterGroupId, IsVisibleToPlayer);
}

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
