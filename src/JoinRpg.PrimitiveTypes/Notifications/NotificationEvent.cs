using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.PrimitiveTypes.Notifications;
/// <summary>
/// Уведомление о каком-то событии 
/// </summary>
public record NotificationEvent(
    NotificationClass NotificationClass,
    IProjectEntityId? EntityReference,
    string Header,
    NotificationEventTemplate TemplateText,
    IReadOnlyCollection<NotificationRecepient> Recepients,
    UserIdentification Initiator);



public record NotificationRecepient
{
    public UserIdentification UserId { get; set; }
    public SubscriptionReason SubscriptionReason { get; set; }
    private readonly IDictionary<string, string> fields;

    public IReadOnlyDictionary<string, string> Fields => fields.AsReadOnly();

    private NotificationRecepient(UserIdentification userId, SubscriptionReason subscriptionReason, IReadOnlyDictionary<string, string>? fields = null)
    {
        UserId = userId;
        SubscriptionReason = subscriptionReason;
        this.fields = fields is null ? [] : new Dictionary<string, string>(fields);
    }

    public NotificationRecepient(UserIdentification userId, string userDisplayName, SubscriptionReason subscriptionReason, IReadOnlyDictionary<string, string>? fields = null)
        : this(userId, subscriptionReason, fields)
    {
        this.fields.TryAdd("name", userDisplayName);
    }

    public NotificationRecepient(UserInfoHeader user, SubscriptionReason subscriptionReason, IReadOnlyDictionary<string, string>? fields = null)
    : this(user.UserId, user.DisplayName.DisplayName, subscriptionReason, fields)
    {
    }
    public NotificationRecepient(ProjectMasterInfo master, IReadOnlyDictionary<string, string>? fields = null)
        : this(master.UserId, master.Name.DisplayName, SubscriptionReason.MasterOfGame, fields)
    {
    }

    public static NotificationRecepient Direct(UserIdentification userId, string userDisplayName, IReadOnlyDictionary<string, string>? Fields = null)
    {
        return new(userId, userDisplayName, SubscriptionReason.DirectToYou, Fields);
    }

    public static NotificationRecepient Player(UserInfoHeader user, IReadOnlyDictionary<string, string>? Fields = null)
    {
        return new(user.UserId, user.DisplayName.DisplayName, SubscriptionReason.Player, Fields ?? new Dictionary<string, string>());
    }

    public static NotificationRecepient Admin(UserInfoHeader user, IReadOnlyDictionary<string, string>? Fields = null)
    {
        return new(user.UserId, user.DisplayName.DisplayName, SubscriptionReason.Admin, Fields ?? new Dictionary<string, string>());
    }
}

public record NotificationEventTemplate(string TemplateContents)
{
    [Obsolete]
    public static implicit operator MarkdownString(NotificationEventTemplate type) => new MarkdownString(type.TemplateContents);
}
