using JoinRpg.Common.WebInfrastructure.Job;
using JoinRpg.Dal.JobService;
using JoinRpg.Services.Impl;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Portal.Infrastructure.Jobs;

public static class JobRegistration
{
    public static void AddJoinJobs(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddJobsDal(configuration, environment);
        services.AddMidnightJobs();
        services
            .AddJob<UpdatePaymentStatusJob>()
            ;
        // PerformRecurrentPaymentMidnightJob намеренно не регистрируется (DRP #5459): она никогда не работала,
        // а после восстановления из старого бэкапа могла бы повторно списать деньги. Без регистрации её нет
        // ни в расписании, ни на /Admin/Jobs. Код джобы и рекуррентных платежей сохранён.
        services.AddJob<EnsureRolesListsJob>();
    }
}
