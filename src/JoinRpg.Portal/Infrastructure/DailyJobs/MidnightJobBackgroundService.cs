using System.Reflection;
using JoinRpg.Common.WebInfrastructure;
using JoinRpg.Common.WebInfrastructure.DailyJob;
using JoinRpg.Interfaces;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

public class MidnightJobBackgroundService<TJob>(
    IServiceProvider serviceProvider,
    ILogger<MidnightJobBackgroundService<TJob>> logger,
    IHostApplicationLifetime hostApplicationLifetime,
    IOptionsMonitor<DailyJobsOptions> options
    ) : BackgroundService
    where TJob : class, IDailyJob
{
    private static readonly string JobName = typeof(TJob).FullName!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await hostApplicationLifetime.WaitForAppStartup(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = typeof(TJob).GetCustomAttribute<JobDelayAttribute>()?.Delay;

            await WaitUntilMidnight(delay, stoppingToken);

            logger.LogInformation("Время запуска джобы... ");
            stoppingToken.ThrowIfCancellationRequested();

            await RunScheduledOnce(DateOnly.FromDateTime(DateTime.Now), stoppingToken);
        }
    }

    /// <summary>
    /// Один плановый запуск: проверка выключения конфигом, запись в журнал DailyJobRuns, запуск джобы.
    /// </summary>
    internal async Task RunScheduledOnce(DateOnly dayOfRun, CancellationToken stoppingToken)
    {
        // Настройка читается на каждом запуске: изменение перезагружаемого конфига подхватывается без рестарта.
        if (options.CurrentValue.IsDisabled(typeof(TJob)))
        {
            // Строку в DailyJobRuns не пишем: после включения джоба отработает на ближайшем плановом запуске.
            logger.LogWarning("Skipping running {jobName}: disabled by configuration {section}:Disabled", JobName, DailyJobsOptions.SectionName);
            return;
        }

        using var scope = serviceProvider.CreateScope();
        using var activity = BackgroundServiceActivity.ActivitySource.StartActivity($"Run of {JobName}");
        activity?.AddTag("jobName", JobName);
        var dailyJobRepository = scope.ServiceProvider.GetRequiredService<IDailyJobRepository>();

        var jobId = new JobId(JobName, dayOfRun);
        if (await dailyJobRepository.TryInsertJobRecord(jobId))
        {
            logger.LogInformation("Will start {jobName} on this instance", JobName);
            try
            {
                var job = scope.ServiceProvider.GetRequiredService<JobRunner<TJob>>();
                await job.RunJob(scope, stoppingToken);
                _ = await dailyJobRepository.TrySetJobCompleted(jobId);
                logger.LogInformation("Successfully complete {jobName} on this instance", JobName);
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
    private async Task WaitUntilMidnight(TimeSpan? delay, CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;
        var midnight = now
            .Date.AddDays(1) // next midnight
            .Add(delay ?? TimeSpan.FromSeconds(5)) // Если ничего нет, запускаем через 5 секунд
            .AddMilliseconds(Random.Shared.Next(5 * 1000)); //let's different pods to start daily jobs at different times
        logger.LogDebug("Время запуска джобы для нас будет {specialMidnight}", midnight);
        await Task.Delay(midnight - now, stoppingToken);
    }
}
