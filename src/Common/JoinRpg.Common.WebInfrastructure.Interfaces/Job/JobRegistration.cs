using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Common.WebInfrastructure.Job;

/// <summary>
/// Регистрация джоб.
/// </summary>
public static class JobRegistration
{
    /// <summary>
    /// Регистрирует джобу: сам класс, раннер с имперсонацией и маркер для планировщика.
    /// Планировщик (в JoinRpg.Common.WebInfrastructure) находит джобы по маркерам при старте,
    /// поэтому порядок вызовов <c>AddJob</c> относительно других регистраций не важен.
    /// </summary>
    public static IServiceCollection AddJob<TJob>(this IServiceCollection services) where TJob : class, IJob
    {
        if (services.Any(d => d.ServiceType == typeof(RegisteredJob) && d.ImplementationInstance is RegisteredJob registered && registered.JobType == typeof(TJob)))
        {
            throw new InvalidOperationException($"Джоба {typeof(TJob).FullName} уже зарегистрирована: повторный AddJob дал бы два планировщика и падающий Single по IJobRunner.");
        }

        return services
            .AddScoped<TJob>()
            .AddScoped<IJobRunner>(sp => sp.GetRequiredService<IJobRunnerFactory>().Create(typeof(TJob)))
            .AddSingleton(new RegisteredJob(typeof(TJob)));
    }
}
