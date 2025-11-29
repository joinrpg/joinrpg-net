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
    /// Base pause before releasing the cooldown.
    /// </summary>
    public TimeSpan BaseCooldownPause { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum number of the subsequent cooldowns before stopping the worker.
    /// </summary>
    public byte MaxSubsequentCooldowns { get; set; } = 5;

    /// <summary>
    /// Hysteresis factor which will be multiplied by random value in range -1..+1 and added to the cooldown pause.
    /// </summary>
    public TimeSpan CooldownHysteresis { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How many subsequent successes should happen to stop counting failures.
    /// </summary>
    public int MinSubsequentSuccessesToStopFailureCounting { get; set; } = 5;

    /// <summary>
    /// How many subsequent successes should happen to stop counting cooldowns.
    /// </summary>
    public int MinSubsequentSuccessesToStopCooldownCounting { get; set; } = 20;
}
