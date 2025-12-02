namespace JoinRpg.PrimitiveTypes.Notifications;

public enum NotificationChannel
{
    ShowInUi,
    Email,
    Telegram
}

public enum NotificationClass
{
    Unknown,
    UserAccount,
    Claims,
    Payment,
    Forum,
    MassProjectEmails,
    /// <summary>
    /// Уведомления, отсылаемые мастерам проекта по какой-то причине
    /// </summary>
    MasterProject,
    AdminMessage
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
    SubscribedDirectMaster,
    MasterOfGame,
    Admin,
}
