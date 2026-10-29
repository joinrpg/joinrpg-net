using System.Text.Json.Serialization;

namespace JoinRpg.Web.Comments;

/// <summary>
/// Обсуждение (заявки или темы форума), готовое к показу текущему пользователю (ADR026 §3).
/// </summary>
/// <remarks>
/// Сериализуемо и без EF: видимость, «новое», дерево ответов, тексты и адреса аватарок уже
/// посчитаны построителем (<c>DiscussionViewModelBuilder</c>). Скрытых от игрока комментариев
/// в модели для игрока нет вовсе.
/// </remarks>
/// <param name="ProjectId">Проект обсуждения — для форм и ссылок.</param>
/// <param name="DiscussionId">Обсуждение — для формы нового комментария.</param>
/// <param name="HasMasterAccess">Текущий пользователь — мастер проекта: может писать скрытые от игрока комментарии.</param>
/// <param name="RootComments">Комментарии верхнего уровня в порядке обсуждения, с ответами внутри.</param>
public record DiscussionViewModel(
    ProjectIdentification ProjectId,
    CommentDiscussionId DiscussionId,
    bool HasMasterAccess,
    IReadOnlyList<CommentViewModel> RootComments);

/// <summary>Комментарий обсуждения с ответами (ADR026 §3).</summary>
/// <param name="CommentId">Комментарий.</param>
/// <param name="Author">Автор — ссылка на профиль.</param>
/// <param name="AuthorAvatarUri">Готовый адрес аватарки автора; <c>null</c>, если её нет.</param>
/// <param name="CreatedAt">Время создания.</param>
/// <param name="IsCommentByPlayer">Написан со стороны игрока, а не мастером.</param>
/// <param name="IsVisibleToPlayer">Виден игроку. Скрытые попадают только в модель для мастера.</param>
/// <param name="IsRead">Текущий пользователь его уже видел (или сам написал).</param>
/// <param name="ExtraActionText">Готовый текст действия с заявкой, которым сопровождался комментарий.</param>
/// <param name="TextHtml">Отрендеренный и очищенный HTML текста; пустая строка, если текста нет.</param>
/// <param name="Finance">Финоперация комментария — данные для слота финблока (только у заявок).</param>
/// <param name="CanConceal">Текущий пользователь может скрыть комментарий от игрока.</param>
/// <param name="Depth">Глубина в дереве: 0 у корней.</param>
/// <param name="Replies">Ответы в порядке обсуждения.</param>
public record CommentViewModel(
    CommentId CommentId,
    UserLinkViewModel Author,
    Uri? AuthorAvatarUri,
    DateTimeOffset CreatedAt,
    bool IsCommentByPlayer,
    bool IsVisibleToPlayer,
    bool IsRead,
    string? ExtraActionText,
    string TextHtml,
    CommentFinanceViewModel? Finance,
    bool CanConceal,
    int Depth,
    IReadOnlyList<CommentViewModel> Replies)
{
    /// <summary>Глубже этого уровня ответы уже не сдвигаются вправо — иначе длинная ветка уезжает за экран.</summary>
    public const int MaxIndentedDepth = 4;

    /// <summary>Сдвигать ли комментарий вправо относительно родителя.</summary>
    [JsonIgnore]
    public bool IsIndented => Depth is > 0 and <= MaxIndentedDepth;
}

/// <summary>
/// Финоперация комментария — минимум для финблока, который подставляет страница заявки
/// (ADR026 §3, шаг 8). Самого финблока в <c>Web.Comments</c> нет.
/// </summary>
/// <param name="OperationText">Что за операция — по типу и знаку суммы; <c>null</c> у операций без суммы.</param>
/// <param name="OperationTypeName">Название типа операции — заголовок диалога модерации.</param>
/// <param name="MoneyAmount">Сумма со знаком.</param>
/// <param name="OperationDate">Дата операции.</param>
/// <param name="PaymentTypeName">Название способа оплаты; <c>null</c>, если способа нет.</param>
/// <param name="IsDeclined">Мастер не подтвердил операцию.</param>
/// <param name="IsProposed">Операция ждёт подтверждения.</param>
/// <param name="CanModerate">Операция требует ручной модерации, и текущий пользователь вправе её провести.</param>
public record CommentFinanceViewModel(
    string? OperationText,
    string OperationTypeName,
    int MoneyAmount,
    DateOnly OperationDate,
    string? PaymentTypeName,
    bool IsDeclined,
    bool IsProposed,
    bool CanModerate);
