using JoinRpg.DataModel;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Services.Interfaces.Notification;

public class NewClaimEmail : ClaimEmailModel, IEmailWithUpdatedFieldsInfo
{
    public IReadOnlyCollection<FieldWithPreviousAndNewValue> UpdatedFields { get; set; } =
        new List<FieldWithPreviousAndNewValue>();

    public IFieldContainter FieldsContainer => Claim;

    public IReadOnlyDictionary<string, PreviousAndNewValue> OtherChangedAttributes { get; } =
        new Dictionary<string, PreviousAndNewValue>();
}

public class FieldsChangedEmail : EmailModelBase, IEmailWithUpdatedFieldsInfo
{
    public IReadOnlyCollection<FieldWithPreviousAndNewValue> UpdatedFields { get; }

    public IFieldContainter FieldsContainer { get; }
    public ILinkable Linkable { get; }

    public IReadOnlyDictionary<string, PreviousAndNewValue> OtherChangedAttributes { get; }

    /// <summary>
    /// Имя связанной заявки
    /// </summary>
    public Claim? Claim { get; }

    /// <summary>
    /// Имя персонажа/заявки
    /// </summary>
    public string Name { get; }

    //Is character is null, Claim is not null and vice versa. (restricted by constructors).
    public bool IsCharacterMail { get; }

    public FieldsChangedEmail(
        Claim claim,
        User initiator,
        ICollection<User> recipients,
        IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields)
        : this(null, claim, initiator, recipients, updatedFields, null)
    {
    }

    [Obsolete("Уберите otherChangedAttributes, это все должно быть в полях")]
    public FieldsChangedEmail(
        Claim claim,
        User initiator,
        ICollection<User> recipients,
        IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields,
        IReadOnlyDictionary<string, PreviousAndNewValue> otherChangedAttributes)
    : this(null, claim, initiator, recipients, updatedFields, otherChangedAttributes)
    {
    }

    public FieldsChangedEmail(
        Character character,
        User initiator,
        ICollection<User> recipients,
        IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields)
        : this(character, null, initiator, recipients, updatedFields, null)
    {
    }

    private FieldsChangedEmail(
        Character? character,
        Claim? claim,
        User initiator,
        ICollection<User> recipients,
        IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields,
        IReadOnlyDictionary<string, PreviousAndNewValue>? otherChangedAttributes)
    {
        if (character != null && claim != null)
        {
            throw new ArgumentException(
                $"Both {nameof(character)} and {nameof(claim)} were provided");
        }

        IsCharacterMail = character != null;

        var targetChar = character ?? claim?.Character ?? throw new ArgumentException($"Neither  {nameof(character)} nor {nameof(claim)} were provided");

        ProjectName = targetChar.Project.ProjectName;
        Name = targetChar.CharacterName;

        FieldsContainer = (IFieldContainter?)claim ?? character ?? throw new ArgumentException($"Neither  {nameof(character)} nor {nameof(claim)} were provided");
        Linkable = (ILinkable?)claim ?? character ?? throw new ArgumentException($"Neither  {nameof(character)} nor {nameof(claim)} were provided");
        Claim = claim ?? targetChar.ApprovedClaim;

        Initiator = initiator;
        Text = new MarkdownString();
        Recipients = recipients;
        UpdatedFields = updatedFields ?? throw new ArgumentNullException(nameof(updatedFields));
        OtherChangedAttributes = otherChangedAttributes ?? new Dictionary<string, PreviousAndNewValue>();
    }
}

public class ClaimEmailModel : EmailModelBase
{
    public ParcipantType InitiatorType { get; set; }
    public Claim Claim { get; set; }
    public CommentExtraAction? CommentExtraAction { get; set; }
}

public enum ParcipantType
{
    Nobody,
    Master,
    Player,
}

public enum ClaimOperationType
{
    PlayerChange,
    MasterVisibleChange,
    MasterSecretChange,
}

public record class ClaimSimpleChangedNotification(
    ClaimIdentification ClaimId,
    UserInfoHeader Player,
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

