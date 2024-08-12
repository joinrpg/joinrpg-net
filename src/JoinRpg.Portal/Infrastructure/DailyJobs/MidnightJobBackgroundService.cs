using JoinRpg.Data.Write.Interfaces;
using JoinRpg.Interfaces;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

public class MidnightJobBackgroundService<TJob>(
    IServiceProvider serviceProvider,
    ILogger<MidnightJobBackgroundService<TJob>> logger,
    IOptions<DailyJobOptions> options
    ) : BackgroundService
    where TJob : class, IDailyJob
{
    private static readonly string JobName = typeof(TJob).FullName!;
    private bool skipWait = options.Value.DebugDailyJobMode;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await WaitUntilMidnight(stoppingToken);
            logger.LogInformation("We are at midnight (special one)");
            stoppingToken.ThrowIfCancellationRequested();

            using var scope = serviceProvider.CreateScope();
            var dailyJobRepository = scope.ServiceProvider.GetRequiredService<IDailyJobRepository>();

            var jobId = new JobId(JobName, DateOnly.FromDateTime(DateTime.Now));
            if (await dailyJobRepository.TryInsertJobRecord(jobId))
            {
                try
                {
                    var job = scope.ServiceProvider.GetRequiredService<TJob>();
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
        if (skipWait)
        {
            skipWait = false;
            return;
        }
        var now = DateTime.Now;
        var midnight = now
            .Date.AddDays(1) // next midnight
            .AddSeconds(5) // 5 seconds later — do not start anything at midnight
            .AddSeconds(Random.Shared.Next(5)); //let's different pods to start daily jobs at different times
        logger.LogDebug("Special midnight will be for us at {specialMidnight}", midnight);
        await Task.Delay(midnight - now, stoppingToken);
    }
}
