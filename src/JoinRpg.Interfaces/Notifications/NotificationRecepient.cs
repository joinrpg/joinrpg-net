using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Interfaces.Notifications;
public record NotificationRecepient(UserIdentification UserId, SubscriptionReason SubscriptionReason, IReadOnlyDictionary<string, string>? Fields = null)
{
    public IReadOnlyDictionary<string, string> UserFields { get; set; } = Fields ?? new Dictionary<string, string>();

    public static NotificationRecepient MasterOfGame(UserIdentification userId, IReadOnlyDictionary<string, string>? Fields = null)
        => new(userId, SubscriptionReason.MasterOfGame, Fields);
    public static NotificationRecepient Player(UserIdentification userId, IReadOnlyDictionary<string, string>? Fields = null)
        => new(userId, SubscriptionReason.Player, Fields);
}

public enum SubscriptionReason
{
    Unknown = 0,
    DirectToYou,
    AnswerToYourComment,
    Player,
    ResponsibleMaster,
    Finance,
    SubscribedMaster,
    MasterOfGame,
}
