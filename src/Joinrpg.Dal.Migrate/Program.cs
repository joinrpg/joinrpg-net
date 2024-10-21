using Joinrpg.Dal.Migrate.Ef6;
using Joinrpg.Dal.Migrate.EfCore;
using JoinRpg.Dal.JobService;
using JoinRpg.Dal.Notifications;
using JoinRpg.Portal.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Joinrpg.Dal.Migrate;

internal class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                _ = services.AddHostedService<OneTimeOperationHostedServiceBase>();

                services.AddScoped<IMigratorService, MigrateMyDbContextService>();

                services.RegisterMigrator<DataProtectionDbContext>(hostContext.Configuration.GetConnectionString("DataProtection")!);
                services.RegisterMigrator<JobScheduleDataDbContext>(hostContext.Configuration.GetConnectionString("DailyJob")!);
                services.RegisterMigrator<NotificationsDataDbContext>(hostContext.Configuration.GetConnectionString("Notifications")!);
            });
}
