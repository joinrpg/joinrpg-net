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
    )
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
    );

