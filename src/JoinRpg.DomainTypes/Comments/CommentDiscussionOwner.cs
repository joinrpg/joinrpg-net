using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes.Comments;

/// <summary>
/// Владелец обсуждения — заявка или тема форума (ADR026 §1). Закрытая иерархия: других
/// вариантов нет, и вне сборки их не завести.
/// </summary>
/// <remarks>
/// Доступ по владельцу проверяют вызывающие. Проект владельца — из его идентификатора: если он не
/// совпал с проектом в маршруте, ответ — 404, иначе мастер одного проекта прошёл бы проверку по
/// обсуждению другого (<see cref="CommentDiscussionId"/> глобально уникален).
/// </remarks>
public abstract record CommentDiscussionOwner
{
    private protected CommentDiscussionOwner()
    {
    }

    /// <summary>Проект, которому принадлежит обсуждение.</summary>
    public abstract ProjectIdentification ProjectId { get; }

    /// <summary>
    /// Адресный id комментария этого обсуждения — для <c>IUriLocator</c> и островов.
    /// </summary>
    public abstract IProjectEntityId GetCommentIdentification(CommentId commentId);
}

/// <summary>Обсуждение заявки.</summary>
public sealed record ClaimDiscussionOwner(ClaimIdentification ClaimId) : CommentDiscussionOwner
{
    public override ProjectIdentification ProjectId => ClaimId.ProjectId;

    public override ClaimCommentIdentification GetCommentIdentification(CommentId commentId)
        => new(ClaimId, commentId.Value);
}

/// <summary>Обсуждение темы форума.</summary>
/// <param name="ThreadId">Тема.</param>
/// <param name="CharacterGroupId">
/// Группа темы. Игрок видит тему, если у него есть принятая заявка на персонажа из этой группы.
/// </param>
/// <param name="IsVisibleToPlayer">Видна ли тема игрокам вообще.</param>
public sealed record ForumDiscussionOwner(
    ForumThreadIdentification ThreadId,
    CharacterGroupIdentification CharacterGroupId,
    bool IsVisibleToPlayer) : CommentDiscussionOwner
{
    // Оба свойства без init: иначе `with` поменял бы одно из них в обход проверки проекта.
    public ForumThreadIdentification ThreadId { get; } = ThreadId;

    public CharacterGroupIdentification CharacterGroupId { get; } = ThreadId.ProjectId == CharacterGroupId.ProjectId
        ? CharacterGroupId
        : throw new ArgumentException("Тема и её группа должны быть из одного проекта.", nameof(CharacterGroupId));

    public override ProjectIdentification ProjectId => ThreadId.ProjectId;

    public override ForumCommentIdentification GetCommentIdentification(CommentId commentId)
        => new(ThreadId, commentId.Value);
}
