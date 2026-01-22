using JoinRpg.Domain.CharacterFields;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.PrimitiveTypes.Users;

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
    );

public record class ClaimOnlinePaymentNotification(
    ClaimIdentification ClaimId,
    UserInfoHeader Player,
    NotificationEventTemplate Text
    );

