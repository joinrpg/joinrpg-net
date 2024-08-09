namespace JoinRpg.Interfaces;

public interface IDailyJob
{
    Task RunOnce(CancellationToken cancellationToken);
}
