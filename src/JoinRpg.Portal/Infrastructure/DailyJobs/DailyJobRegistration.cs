using JoinRpg.Dal.JobService;
using JoinRpg.Interfaces;
using JoinRpg.Services.Impl;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

public static class DailyJobRegistration
{
    public static void AddJoinDailyJob(this IJoinServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddDailyJobsDal(configuration, environment);
        _ = services.AddOptions<DailyJobsOptions>().BindConfiguration(DailyJobsOptions.SectionName);
        _ = services.AddHostedService<DailyJobsOptionsCheck>();
        //TODO invent way to construct every implementation of IDailyJob
        _ = services
            .AddDailyJob<UpdatePaymentStatusJob>()
            ;
        // PerformRecurrentPaymentMidnightJob намеренно не регистрируется (DRP #5459): она никогда не работала,
        // а после восстановления из старого бэкапа могла бы повторно списать деньги. Без регистрации её нет
        // ни в расписании, ни на /Admin/Jobs. Код джобы и рекуррентных платежей сохранён.
        services.AddDailyJob<EnsureRolesListsJob>();
    }
}
