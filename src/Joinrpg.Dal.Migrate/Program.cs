using JoinRpg.Common.WebInfrastructure.DataProtection;
using JoinRpg.Common.WebInfrastructure.EfCoreMigration;
using JoinRpg.Dal.JobService;
using JoinRpg.Dal.Migrate.Ef6;
using JoinRpg.Dal.Notifications;
using JoinRpg.IdPortal.OAuthServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.Dal.Migrate;

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
                _ = services.AddHostedService<MigrationsLauncher>();

                services.AddScoped<IMigratorService, MigrateMyDbContextService>();
                var configuration = hostContext.Configuration;
                var environment = hostContext.HostingEnvironment;


                services.RegisterMigrator<DataProtectionDbContext>(configuration, environment, "DataProtection");
                services.RegisterMigrator<JobScheduleDataDbContext>(configuration, environment, "DailyJob");
                services.RegisterMigrator<NotificationsDataDbContext>(configuration, environment, "Notifications");
                services.RegisterMigrator<IdPortalDbContext>(configuration, environment, "IdPortal", options => options.UseOpenIddict());
            });
}
