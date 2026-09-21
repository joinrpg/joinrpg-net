using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Notifications;

namespace JoinRpg.Services.Impl.Claims;

public enum ClaimOperationType
{
    PlayerChange,
    MasterVisibleChange,
    MasterSecretChange,
}

/// <summary>
/// Общее у всех уведомлений по заявке: к какой заявке относится и текст события. Нужен, чтобы
/// разнотипные уведомления одной операции можно было сложить в одну очередь и разослать в порядке
/// добавления (ADR014).
/// </summary>
public interface IClaimNotification
{
    ClaimIdentification ClaimId { get; }

    NotificationEventTemplate Text { get; }
}

public record class ClaimSimpleChangedNotification(
    ClaimIdentification ClaimId,
    CommentExtraAction? CommentExtraAction,                                 // Не нравится, что тут nullable
    UserInfoHeader Initiator,
    NotificationEventTemplate Text,
    ClaimOperationType ClaimOperationType,
    UserIdentification? OldResponsibleMaster = null,                        // Мог поменяться
    CharacterIdentification? AnotherCharacterId = null,                          // Это если
    int? Money = null,
    UserInfoHeader? PaymentOwner = null,
    UserInfoHeader? ParentCommentAuthor = null,
    IReadOnlyCollection<FieldWithPreviousAndNewValue>? UpdatedFields = null
    ) : IClaimNotification
{
    /// <summary>
    /// Идентификатор комментария становится известен только после сохранения в базу,
    /// поэтому заполняется уже после <c>SaveChangesAsync</c>.
    /// </summary>
    public ClaimCommentIdentification? CommentId { get; init; }

    public ClaimSimpleChangedNotification WithCommentId(int commentId)
        => this with { CommentId = new ClaimCommentIdentification(ClaimId, commentId) };
}

public record class ClaimOnlinePaymentNotification(
    ClaimIdentification ClaimId,
    UserInfoHeader Player,
    NotificationEventTemplate Text,
    FinanceOperationIdentification FinanceOperationId
    ) : IClaimNotification;

