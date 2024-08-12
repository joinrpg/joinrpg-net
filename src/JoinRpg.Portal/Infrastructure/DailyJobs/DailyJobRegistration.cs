using JoinRpg.Dal.JobService;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

public static class DailyJobRegistration
{
    public static void AddJoinDailyJob(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddOptions<DailyJobOptions>();
        services.AddJoinEfCoreDbContext<JobScheduleDataDbContext>(configuration, environment, "DailyJob");
        services.AddTransient<IDailyJobRepository, DailyJobRepository>();
        //TODO invent way to construct every implementation of IDailyJob
        services.AddDailyJob<DoNothingMidnightJob>();

        services
            .AddDailyJob<ProjectWarnCloseJob>()
            .AddDailyJob<ProjectPerformCloseJob>();
    }

    public static IServiceCollection AddDailyJob<TJob>(this IServiceCollection services) where TJob : class, IDailyJob
    {
        return services
            .AddScoped<TJob>()
            .AddScoped<IDailyJob, TJob>()
            .AddHostedService<MidnightJobBackgroundService<TJob>>();
    }
}
