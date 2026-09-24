using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Комментарий, созданный внутри мутации, вместе с уведомлением, которое уйдёт ПОСЛЕ сохранения
/// (ADR014).
/// </summary>
/// <remarks>
/// Разделение нужно потому, что уведомление ссылается на <c>CommentId</c>, а он появляется только
/// после <c>SaveChanges</c>. До миграции это приводило к одиннадцати копиям связки
/// «создать комментарий → сохранить → отправить с подставленным id» в <c>ClaimServiceImpl</c>.
/// Класс, а не record: <see cref="Notification"/> декорируется по месту, а в пути создания заявки
/// сам комментарий появляется позже.
/// </remarks>
/// <summary>
/// Заказ на комментарий: операция создания заявки его попросила, но самого
/// <see cref="Comment"/> ещё нет. Предыдущая стадия того же пути, что и
/// <see cref="PendingComment"/>, а не его дубль.
/// </summary>
/// <remarks>
/// <see cref="PendingComment"/> держит уже созданный комментарий и ждёт только его
/// <c>CommentId</c>. Здесь ждать приходится раньше: <c>CommentHelper</c> копирует в комментарий
/// <c>CommentDiscussionId</c> значением, а у дискуссии новой заявки до первого сохранения он
/// равен <c>-1</c>. Поэтому в <c>CharacterPropsService.CreateClaim</c> заказ превращается в
/// <see cref="PendingComment"/> между двумя сохранениями. До миграции ровно та же пара сохранений
/// стояла в <c>AddClaimFromUser</c> с комментарием «CreateClaimCommentWithNotification ожидает,
/// что заявка уже существует».
/// </remarks>
internal sealed record DeferredComment(
    string CommentText,
    CommentExtraAction? ExtraAction,
    ClaimOperationType OperationType);

internal sealed class PendingComment(Comment comment, ClaimSimpleChangedNotification notification)
{
    /// <summary>Созданный комментарий. Его <c>CommentId</c> известен только после сохранения.</summary>
    public Comment Comment { get; } = comment;

    /// <summary>Уведомление, которое уйдёт после сохранения.</summary>
    public ClaimSimpleChangedNotification Notification { get; private set; } = notification;

    /// <summary>Уведомление создаётся, но не отправляется.</summary>
    public bool IsSilent { get; private set; }

    /// <summary>
    /// Дополняет уведомление данными, известными операции: другим персонажем при переносе,
    /// прежним ответственным мастером, суммой и владельцем типа оплаты.
    /// </summary>
    public PendingComment Decorate(Func<ClaimSimpleChangedNotification, ClaimSimpleChangedNotification> decorator)
    {
        Notification = decorator(Notification);
        return this;
    }

    /// <summary>
    /// Отмечает комментарий ответом на <paramref name="parentComment"/>. Переносит проверку
    /// «нельзя ответить на скрытый от игрока комментарий так, чтобы игрок ответ увидел».
    /// </summary>
    public PendingComment SetParent(Comment parentComment, ClaimOperationType claimOperationType)
    {
        if (claimOperationType != ClaimOperationType.MasterSecretChange && !parentComment.IsVisibleToPlayer)
        {
            throw new EntityWrongStatusException(parentComment);
        }

        Comment.Parent = parentComment;

        return Decorate(notification => notification with
        {
            ParentCommentAuthor = parentComment.Author.ToUserInfoHeader(),
            PaymentOwner = parentComment.Finance?.PaymentType?.User?.ToUserInfoHeader(),
        });
    }

    /// <summary>
    /// Комментарий создаётся, но уведомление по нему не отправляется. Нужен там, где так было и до
    /// миграции, — иначе игроки начнут получать письма, которых раньше не было.
    /// </summary>
    public PendingComment Silent()
    {
        IsSilent = true;
        return this;
    }
}
