using JoinRpg.Dal.JobService;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.Interfaces;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

public static class DailyJobRegistration
{
    public static void AddJoinDailyJob(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddJoinEfCoreDbContext<JobScheduleDataDbContext>(configuration, environment, "DailyJob");
        services.AddTransient<IDailyJobRepository, DailyJobRepository>();
        //TODO invent way to construct every implementation of IDailyJob
        services.AddDailyJob<DoNothingMidnightJob>();
    }

    public static IServiceCollection AddDailyJob<TJob>(this IServiceCollection services) where TJob : class, IDailyJob
    {
        return services
            .AddScoped<TJob>()
            .AddHostedService<MidnightJobBackgroundService<TJob>>();
    }
}
