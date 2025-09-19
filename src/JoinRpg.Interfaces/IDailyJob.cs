namespace JoinRpg.Interfaces;

public interface IDailyJob
{
    Task RunOnce(CancellationToken cancellationToken);
}

[AttributeUsage(AttributeTargets.Class)]
public class JobDelayAttribute(int hoursDelay) : Attribute
{
    public TimeSpan Delay { get; } = TimeSpan.FromHours(hoursDelay);
}
