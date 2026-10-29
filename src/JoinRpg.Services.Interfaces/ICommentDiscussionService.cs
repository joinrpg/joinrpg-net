namespace JoinRpg.Services.Interfaces;

/// <summary>
/// Запись в обсуждение — ленту комментариев заявки или темы форума (ADR026 §5). Единый вход: какой
/// владелец у обсуждения, сервис находит сам.
/// </summary>
/// <remarks>
/// Объединён вход, а не реализация: комментарий к заявке по-прежнему операция агрегата персонажа
/// (ADR014) со своими побочными эффектами — времена заявки, перевод в «обсуждается», уведомление по
/// заявке; комментарий к теме — операция форума.
/// </remarks>
public interface ICommentDiscussionService
{
    /// <summary>Обычный комментарий — без действия над заявкой.</summary>
    /// <param name="discussionId">Обсуждение.</param>
    /// <param name="parentCommentId">Комментарий, на который это ответ; должен быть в этом же обсуждении.</param>
    /// <param name="isVisibleToPlayer">
    /// Виден ли комментарий игроку. Скрыть может только мастер, и ответ на скрытый от игрока комментарий
    /// тоже должен быть скрытым.
    /// </param>
    /// <param name="commentText">Текст.</param>
    Task AddComment(CommentDiscussionId discussionId, CommentId? parentCommentId, bool isVisibleToPlayer, string commentText);

    /// <summary>Скрывает от игрока комментарий мастера, на который ещё нет ответов.</summary>
    Task ConcealComment(CommentId commentId);

    /// <summary>Отметка «прочитано до такого-то комментария» для текущего пользователя.</summary>
    Task UpdateReadCommentWatermark(CommentDiscussionId discussionId, CommentId maxCommentId);
}
