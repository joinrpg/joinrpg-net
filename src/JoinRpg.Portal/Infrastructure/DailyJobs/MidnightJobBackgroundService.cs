using JoinRpg.Data.Write.Interfaces;
using JoinRpg.Interfaces;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

public class MidnightJobBackgroundService<TJob>(IDailyJobRepository dailyJobRepository, IServiceProvider serviceProvider, ILogger<MidnightJobBackgroundService<TJob>> logger) : BackgroundService
    where TJob : class, IDailyJob
{
    private static readonly string JobName = typeof(TJob).FullName!;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await WaitUntilMidnight(stoppingToken);
            logger.LogInformation("We are at midnight (special one)");
            stoppingToken.ThrowIfCancellationRequested();
            var jobId = new JobId(JobName, DateOnly.FromDateTime(DateTime.Now));
            if (await dailyJobRepository.TryInsertJobRecord(jobId))
            {
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var job = serviceProvider.GetRequiredService<TJob>();
                    await job.RunOnce(stoppingToken);
                    _ = await dailyJobRepository.TrySetJobCompleted(jobId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error running {jobName}", JobName);
                    _ = await dailyJobRepository.TrySetJobFailed(jobId);
                }
            }
            else
            {
                logger.LogInformation("Skipping running {jobName} as it already running at another instance", JobName);
            }
        }
    }
    private async Task WaitUntilMidnight(CancellationToken stoppingToken)
    {
        var now = DateTime.Now;
        var midnight = now
            .Date.AddDays(1) // next midnight
            .AddSeconds(5) // 5 seconds later — do not start anything at midnight
            .AddSeconds(Random.Shared.Next(5)); //let's different pods to start daily jobs at different times
        logger.LogDebug("Special midnight will be for us at {specialMidnight}", midnight);
        await Task.Delay(midnight - now, stoppingToken);
    }
}
