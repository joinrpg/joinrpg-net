using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using JoinRpg.Dal.JobService;
using JoinRpg.Dal.Migrate.EfCore;
using JoinRpg.Dal.Migrate.Ef6;
using JoinRpg.Portal.Infrastructure;

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

                services.RegisterMigrator<DataProtectionDbContext>(hostContext.Configuration.GetConnectionString("DataProtection")!);
                services.RegisterMigrator<JobScheduleDataDbContext>(hostContext.Configuration.GetConnectionString("DailyJob")!);
            });
}
