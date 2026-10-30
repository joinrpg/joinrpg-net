using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoinRpg.Common.WebInfrastructure.Job;

/// <summary>
/// Единый планировщик ежедневных джоб: на старте находит все зарегистрированные джобы
/// (маркеры <see cref="RegisteredJob"/> из <c>AddJob</c>) и для каждой ждёт свою полночь UTC.
/// Порядок вызовов <c>AddJob</c> при конфигурации сервисов не важен.
/// </summary>
internal class MidnightJobsScheduler(
    IServiceProvider serviceProvider,
    ILogger<MidnightJobsScheduler> logger,
    IHostApplicationLifetime hostApplicationLifetime,
    IOptionsMonitor<JobsOptions> options,
    TimeProvider timeProvider,
    IEnumerable<RegisteredJob> registeredJobs) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await hostApplicationLifetime.WaitForAppStartup(stoppingToken);
        await Task.WhenAll(registeredJobs.Select(job => RunJobLoop(job.JobType, stoppingToken)));
    }

    private async Task RunJobLoop(Type jobType, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = jobType.GetCustomAttribute<JobDelayAttribute>()?.Delay;

            var dayOfRun = await WaitUntilMidnight(delay, stoppingToken);

            logger.LogInformation("Время запуска джобы... ");
            stoppingToken.ThrowIfCancellationRequested();

            await RunScheduledOnce(jobType, dayOfRun, stoppingToken);
        }
    }

    /// <summary>
    /// Один плановый запуск: проверка выключения конфигом, запись в журнал DailyJobRuns, запуск джобы.
    /// </summary>
    internal async Task RunScheduledOnce(Type jobType, DateOnly dayOfRun, CancellationToken stoppingToken)
    {
        var jobName = jobType.FullName!;

        // Настройка читается на каждом запуске: изменение перезагружаемого конфига подхватывается без рестарта.
        if (options.CurrentValue.IsDisabled(jobType))
        {
            // Строку в DailyJobRuns не пишем: после включения джоба отработает на ближайшем плановом запуске.
            logger.LogWarning("Skipping running {jobName}: disabled by configuration {section}:Disabled", jobName, JobsOptions.SectionName);
            return;
        }

        using var scope = serviceProvider.CreateScope();
        using var activity = BackgroundServiceActivity.ActivitySource.StartActivity($"Run of {jobName}");
        activity?.AddTag("jobName", jobName);
        var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        var jobId = new JobId(jobName, dayOfRun);
        if (await jobRepository.TryInsertJobRecord(jobId))
        {
            logger.LogInformation("Will start {jobName} on this instance", jobName);
            try
            {
                var jobRunner = scope.ServiceProvider.GetRequiredService<IEnumerable<IJobRunner>>().Single(r => r.JobType == jobType);
                await jobRunner.RunJob(scope, stoppingToken);
                _ = await jobRepository.TrySetJobCompleted(jobId);
                logger.LogInformation("Successfully complete {jobName} on this instance", jobName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running {jobName}", jobName);
                _ = await jobRepository.TrySetJobFailed(jobId);
            }
        }
        else
        {
            logger.LogInformation("Skipping running {jobName} as it already running at another instance", jobName);
        }
    }

    private async Task<DateOnly> WaitUntilMidnight(TimeSpan? delay, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();
        var (dayOfRun, startAt) = GetNextRun(
            now,
            delay,
            TimeSpan.FromMilliseconds(Random.Shared.Next(5 * 1000))); //let's different pods to start daily jobs at different times
        logger.LogDebug("Время запуска джобы для нас будет {specialMidnight}", startAt);
        await Task.Delay(startAt - now, timeProvider, stoppingToken);
        return dayOfRun;
    }

    /// <summary>
    /// Следующий плановый запуск. И момент запуска, и день для журнала DailyJobRuns считаются по UTC,
    /// а не по таймзоне контейнера: иначе экземпляры с разной таймзоной запишут один запуск под разными днями,
    /// и блокировка «один запуск в сутки» не сработает.
    /// </summary>
    internal static (DateOnly DayOfRun, DateTimeOffset StartAt) GetNextRun(DateTimeOffset now, TimeSpan? delay, TimeSpan jitter)
    {
        var midnight = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero); // next midnight UTC
        var startAt = midnight
            .Add(delay ?? TimeSpan.FromSeconds(5)) // Если ничего нет, запускаем через 5 секунд
            .Add(jitter);
        return (DateOnly.FromDateTime(midnight.UtcDateTime), startAt);
    }
}
