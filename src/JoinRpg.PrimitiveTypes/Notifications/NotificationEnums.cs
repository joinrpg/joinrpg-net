namespace JoinRpg.PrimitiveTypes.Notifications;

public enum NotificationChannel
{
    ShowInUi,
    Email,
}

public enum NotificationClass
{
    Unknown,
    UserAccount,
    Claims,
    Payment,
    Forum,
    MassProjectEmails,
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
