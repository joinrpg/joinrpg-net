using JoinRpg.DomainTypes.Comments;

namespace JoinRpg.Data.Interfaces.Comments;

/// <summary>
/// Загрузка обсуждений — комментариев заявок и тем форума — доменными снимками (ADR026 §2).
/// </summary>
/// <remarks>
/// Доступ не проверяется: проверяют вызывающие, по владельцу обсуждения. Проект владельца надо
/// сверить с проектом из маршрута — id обсуждения и комментария глобально уникальны.
/// </remarks>
public interface ICommentDiscussionRepository
{
    /// <summary>
    /// Обсуждение целиком, со всеми комментариями и владельцем. <c>null</c>, если такого обсуждения нет.
    /// </summary>
    Task<CommentDiscussionInfo?> GetDiscussionOrDefault(CommentDiscussionId discussionId);

    /// <summary>Владелец обсуждения, без комментариев. <c>null</c>, если такого обсуждения нет.</summary>
    Task<CommentDiscussionOwner?> GetOwnerOrDefault(CommentDiscussionId discussionId);

    /// <summary>
    /// Владелец обсуждения, в котором лежит комментарий, без комментариев. <c>null</c>, если такого
    /// комментария нет.
    /// </summary>
    /// <remarks>
    /// Отдельного метода для финоперации нет: её ключ и есть id комментария.
    /// </remarks>
    Task<CommentDiscussionOwner?> GetOwnerByCommentOrDefault(CommentId commentId);

    /// <summary>
    /// Отметка прочтения пользователя в обсуждении — последний комментарий, который он видел.
    /// <c>null</c>, если отметки нет. Отметок на пару бывает несколько — берётся максимальная.
    /// </summary>
    /// <remarks>
    /// Только явные отметки. Собственные комментарии пользователя тоже считаются прочитанными, но
    /// они уже есть в снимке обсуждения, поэтому сюда не входят.
    /// </remarks>
    Task<CommentId?> GetReadWatermark(CommentDiscussionId discussionId, UserIdentification userId);
}
