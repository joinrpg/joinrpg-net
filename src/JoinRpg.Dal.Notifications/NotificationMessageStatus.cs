namespace JoinRpg.Dal.Notifications;

/// <summary>
/// Describes possible states of a message.
/// </summary>
public enum NotificationMessageStatus
{
    /// <summary>
    /// The message has been enqueued and awaiting processing.
    /// </summary>
    /// <remarks>Initial status.</remarks>
    Queued = 0,

    /// <summary>
    /// The message is being sent.
    /// </summary>
    /// <remarks>Intermediate status.</remarks>
    Sending = 1,

    /// <summary>
    /// Sending succeeded.
    /// </summary>
    /// <remarks>Final status.</remarks>
    Sent = 2,

    /// <summary>
    /// Sending failed.
    /// </summary>
    /// <remarks>Final status. Failure reason require investigation.</remarks>
    Failed = 3,
}
