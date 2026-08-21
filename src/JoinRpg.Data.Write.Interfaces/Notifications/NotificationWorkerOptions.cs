namespace JoinRpg.Data.Write.Interfaces.Notifications;

public class NotificationWorkerOptions
{
    /// <summary>
    /// How long to wait before checking for the next message when previous check showed the empty queue.
    /// </summary>
    public TimeSpan EmptyPause { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Maximum number of attempts to send a message.
    /// </summary>
    public byte MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Base pause before the next sending attempt. Actual pause is calculated as <c>attempt^2 * BasePause</c>.
    /// </summary>
    public TimeSpan BaseAttemptsPause { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Value in range 0..1 which being multiplied with delay allows to get hysteresis
    /// which then will be multiplied by random value in range -0.5..+0.5
    /// and added to the delay.
    /// </summary>
    public double HysteresisFactor { get; set; } = 0.1;

    /// <summary>
    /// Maximum numbers of the subsequent failures before the cooldown.
    /// </summary>
    public byte MaxSubsequentFailures { get; set; } = 50;

    /// <summary>
    /// Pause before releasing the first cooldown. Each subsequent cooldown doubles the previous one.
    /// </summary>
    public TimeSpan BaseCooldownPause { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum allowed cooldown pause. Once the next computed cooldown would exceed this value,
    /// the worker shuts down instead of cooling down again.
    /// </summary>
    public TimeSpan MaxCooldownPause { get; set; } = TimeSpan.FromHours(48);

    /// <summary>
    /// How many subsequent successes should happen to stop counting failures.
    /// </summary>
    public int MinSubsequentSuccessesToStopFailureCounting { get; set; } = 5;

    /// <summary>
    /// How many subsequent successes should happen to stop counting cooldowns.
    /// </summary>
    public int MinSubsequentSuccessesToStopCooldownCounting { get; set; } = 2;

    /// <summary>
    /// How often to poll notification queue lengths for metrics.
    /// </summary>
    public TimeSpan QueueMetricsPollInterval { get; set; } = TimeSpan.FromSeconds(30);
}
