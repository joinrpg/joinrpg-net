using JoinRpg.Dal.CommonEfCore;
using JoinRpg.Data.Write.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.Dal.JobService;
public static class Registrations
{
    public static void AddDailyJobsDal(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddJoinEfCoreDbContext<JobScheduleDataDbContext>(configuration, environment, "DailyJob");
        services.AddTransient<IDailyJobRepository, DailyJobRepository>();
    }
}
