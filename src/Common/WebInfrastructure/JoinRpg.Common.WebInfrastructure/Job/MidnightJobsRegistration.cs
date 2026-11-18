using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JoinRpg.Common.WebInfrastructure.Job;

public static class MidnightJobsRegistration
{
    /// <summary>
    /// Регистрирует единый планировщик ежедневных джоб: ждёт полночь UTC для каждой джобы,
    /// зарегистрированной через <c>AddJob</c> (пакет JoinRpg.Common.WebInfrastructure.Interfaces),
    /// и запускает её at-most-once в сутки через <see cref="IJobRepository"/>.
    /// Вызвать один раз в composition root; порядок относительно <c>AddJob</c> не важен.
    /// </summary>
    public static IServiceCollection AddMidnightJobs(this IServiceCollection services)
    {
        _ = services.AddOptions<JobsOptions>().BindConfiguration(JobsOptions.SectionName);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IJobRunnerFactory, JobRunnerFactory>();
        _ = services.AddHostedService<JobsOptionsCheck>();
        _ = services.AddHostedService<MidnightJobsScheduler>();
        return services;
    }
}
