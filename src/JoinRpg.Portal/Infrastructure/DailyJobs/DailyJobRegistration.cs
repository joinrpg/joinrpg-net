using JoinRpg.Dal.JobService;
using JoinRpg.Interfaces;
using JoinRpg.Services.Impl;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

public static class DailyJobRegistration
{
    public static void AddJoinDailyJob(this IJoinServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddOptions<DailyJobOptions>();
        services.AddDailyJobsDal(configuration, environment);
        //TODO invent way to construct every implementation of IDailyJob
        services.AddDailyJob<DoNothingMidnightJob>();

        _ = services
            .AddDailyJob<UpdatePaymentStatusJob>()
            ;
        services.AddDailyJob<PerformRecurrentPaymentMidnightJob>();
    }
}
