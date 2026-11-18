namespace JoinRpg.Web.Comments;

/// <summary>
/// Адреса для компонентов обсуждения: куда отправлять формы и ссылка на отдельный комментарий.
/// </summary>
/// <remarks>
/// Компоненты обсуждения рендерятся статически (ADR026 §3), поэтому реализация нужна только
/// серверная (<c>UriServiceImpl</c>). Если обсуждение когда-нибудь станет островом, понадобится и
/// клиентская.
/// </remarks>
public interface ICommentUriLocator
{
    /// <summary>
    /// Куда отправлять новый комментарий и ответ — одна точка для заявок и форума
    /// (<c>ForumController.CreateComment</c>).
    /// </summary>
    Uri GetCreateCommentUri(ProjectIdentification projectId);

    /// <summary>Куда отправлять скрытие комментария от игрока (<c>ForumController.ConcealComment</c>).</summary>
    Uri GetConcealCommentUri(ProjectIdentification projectId);

    /// <summary>Постоянная ссылка на комментарий — переход к нему из любого места.</summary>
    Uri GetCommentUri(ProjectIdentification projectId, CommentId commentId);
}
