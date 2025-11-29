using JoinRpg.DataModel;

namespace JoinRpg.PrimitiveTypes.Notifications;
/// <summary>
/// Уведомление о каком-то событии 
/// </summary>
public record NotificationEvent(
    NotificationClass NotificationClass,
    ProjectIdentification? Project,
    string Header,
    MarkdownString TemplateText,
    NotificationRecepient[] Recepients,
    UserIdentification Initiator);



public record NotificationRecepient(UserIdentification UserId, SubscriptionReason SubscriptionReason, IReadOnlyDictionary<string, string> Fields)
{
    public static NotificationRecepient MasterOfGame(UserIdentification userId, IReadOnlyDictionary<string, string>? Fields = null)
        => new(userId, SubscriptionReason.MasterOfGame, Fields ?? new Dictionary<string, string>());
    public static NotificationRecepient Player(UserIdentification userId, IReadOnlyDictionary<string, string>? Fields = null)
        => new(userId, SubscriptionReason.Player, Fields ?? new Dictionary<string, string>());
}

