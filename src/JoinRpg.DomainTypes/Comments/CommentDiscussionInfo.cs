using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.DomainTypes.Comments;

/// <summary>
/// Доменный снимок обсуждения — ленты комментариев заявки или темы форума (ADR026 §1).
/// </summary>
/// <remarks>
/// Снимок не зависит от пользователя: видимость комментариев и «прочитано» считаются снаружи,
/// отметка прочтения грузится отдельно (<c>ICommentDiscussionRepository.GetReadWatermark</c>).
/// </remarks>
/// <param name="Id">Обсуждение.</param>
/// <param name="Owner">Заявка или тема форума, которой принадлежит обсуждение.</param>
/// <param name="Comments">Комментарии обсуждения в любом порядке — снимок упорядочит их сам.</param>
public record CommentDiscussionInfo(
    CommentDiscussionId Id,
    CommentDiscussionOwner Owner,
    IReadOnlyList<CommentInfo> Comments)
{
    /// <summary>
    /// Все комментарии обсуждения (и корни, и ответы) по <see cref="CommentInfo.CreatedAt"/>, при
    /// равенстве — по <see cref="CommentInfo.Id"/>.
    /// </summary>
    /// <remarks>
    /// Только по id упорядочивать нельзя: комментарии, восстановленные DRP без исходного id,
    /// получают новый id при старом времени. Только по времени тоже: старые комментарии форума
    /// хранят одну дату без времени (ADR026, PR 2c), и порядок внутри дня держит id.
    /// </remarks>
    public IReadOnlyList<CommentInfo> Comments { get; } =
        [.. Comments.OrderBy(comment => comment.CreatedAt).ThenBy(comment => comment.Id.Value)];
}

/// <summary>Комментарий обсуждения (ADR026 §1).</summary>
/// <param name="Id">Комментарий.</param>
/// <param name="ParentCommentId">Комментарий, на который это ответ; <c>null</c> у корней.</param>
/// <param name="Author">Автор. Имя и аватарку вызывающий берёт пачкой по всем авторам.</param>
/// <param name="CreatedAt">Время создания. У старых комментариев форума — только дата.</param>
/// <param name="IsCommentByPlayer">Написан игроком (со стороны игрока), а не мастером.</param>
/// <param name="IsVisibleToPlayer">Виден игроку. Скрытые видят только мастера.</param>
/// <param name="ExtraAction">Действие с заявкой, которым сопровождался комментарий.</param>
/// <param name="Text">Текст. В БД бывает пустым — например, у комментария-действия.</param>
/// <param name="Finance">Финоперация, если комментарий её сопровождает (только у заявок).</param>
public record CommentInfo(
    CommentId Id,
    CommentId? ParentCommentId,
    UserIdentification Author,
    DateTime CreatedAt,
    bool IsCommentByPlayer,
    bool IsVisibleToPlayer,
    CommentExtraAction? ExtraAction,
    MarkdownString? Text,
    CommentFinanceInfo? Finance);

/// <summary>
/// Финоперация комментария — мини-снимок для показа в обсуждении, а не доменная лента финансов
/// (ADR026 §1, #5420). Ключ операции — id её комментария.
/// </summary>
/// <param name="MoneyAmount">Сумма.</param>
/// <param name="OperationType">Тип операции.</param>
/// <param name="State">Состояние модерации.</param>
/// <param name="OperationDate">Дата операции — как в БД, без часового пояса.</param>
/// <param name="PaymentType">
/// Способ оплаты; название разрешается через <c>ProjectInfo</c>. <c>null</c> у операций без способа
/// оплаты (льготный взнос, перевод между заявками и т.п.).
/// </param>
public record CommentFinanceInfo(
    int MoneyAmount,
    FinanceOperationType OperationType,
    FinanceOperationState State,
    DateTime OperationDate,
    PaymentTypeIdentification? PaymentType)
{
    /// <summary>
    /// Операция ждёт ручного решения мастера: предложенная оплата наличными или своим способом и запрос
    /// льготного взноса. Онлайн-оплату в состоянии «предложена» подтверждает банк, а не мастер.
    /// </summary>
    /// <remarks>
    /// То же правило, что <c>FinanceOperation.RequireModeration</c> в DataModel. Кто вправе её
    /// подтвердить — отдельно, по <c>ProjectInfo.CanModerateFinanceOperation</c>.
    /// </remarks>
    public bool RequireModeration =>
        State == FinanceOperationState.Proposed
        && OperationType is FinanceOperationType.Submit or FinanceOperationType.PreferentialFeeRequest;
}
