using JoinRpg.Interfaces;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

public class DoNothingMidnightJob(ILogger<DoNothingMidnightJob> logger) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        logger.LogInformation("Do nothing on one pod. At most once");
        await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
    }
}
