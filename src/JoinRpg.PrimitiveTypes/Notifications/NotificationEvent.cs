using JoinRpg.DataModel;

namespace JoinRpg.PrimitiveTypes.Notifications;
/// <summary>
/// Уведомление о каком-то событии 
/// </summary>
public record NotificationEvent(
    NotificationClass NotificationClass,
    IProjectEntityId? EntityReference,
    string Header,
    NotificationEventTemplate TemplateText,
    NotificationRecepient[] Recepients,
    UserIdentification Initiator);



public record NotificationRecepient(UserIdentification UserId, SubscriptionReason SubscriptionReason, IReadOnlyDictionary<string, string> Fields)
{
    public static NotificationRecepient MasterOfGame(UserIdentification userId, IReadOnlyDictionary<string, string>? Fields = null)
        => new(userId, SubscriptionReason.MasterOfGame, Fields ?? new Dictionary<string, string>());

    public static NotificationRecepient Direct(UserIdentification userId, IReadOnlyDictionary<string, string>? Fields = null)
    => new(userId, SubscriptionReason.DirectToYou, Fields ?? new Dictionary<string, string>());
    public static NotificationRecepient Player(UserIdentification userId, IReadOnlyDictionary<string, string>? Fields = null)
        => new(userId, SubscriptionReason.Player, Fields ?? new Dictionary<string, string>());
}

public record NotificationEventTemplate(string TemplateContents)
{
    [Obsolete]
    public static implicit operator MarkdownString(NotificationEventTemplate type) => new MarkdownString(type.TemplateContents);
}
