using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

/// <summary>
/// При старте предупреждает об именах в <see cref="DailyJobsOptions.Disabled"/>, которым не соответствует ни одна джоба.
/// Не падает: опечатка в конфиге не должна валить сайт.
/// </summary>
internal class DailyJobsOptionsCheck(
    IEnumerable<RegisteredDailyJob> registeredJobs,
    IOptionsMonitor<DailyJobsOptions> options,
    ILogger<DailyJobsOptionsCheck> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var jobTypes = registeredJobs.Select(j => j.JobType).ToList();
        var current = options.CurrentValue;

        var unknown = current.GetUnknownNames(jobTypes);
        if (unknown.Count > 0)
        {
            logger.LogWarning(
                "В настройке {section}:Disabled указаны неизвестные джобы {unknownJobs}. Известные джобы: {knownJobs}",
                DailyJobsOptions.SectionName,
                unknown,
                jobTypes.Select(t => t.Name).Order().ToList());
        }

        var disabled = jobTypes.Where(current.IsDisabled).Select(t => t.Name).ToList();
        if (disabled.Count > 0)
        {
            logger.LogWarning("Ежедневные джобы выключены конфигом: {disabledJobs}", disabled);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
