using Joinrpg.Dal.Migrate.EfCore;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Dal.Impl;
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
                services.RegisterMigrator<DataProtectionDbContext>(hostContext.Configuration.GetConnectionString("DataProtection"));

                services.AddSingleton<IJoinDbContextConfiguration>(
                    new DbContextConfiguration
                    {
                        ConnectionString = hostContext.Configuration.GetConnectionString(DbConsts.DefaultConnection),
                        DetailedErrors = true,
                        SensitiveLogging = false, // Logs of migrator are publicly available 
                    });
                services.AddDbContext<MyDbContext>();
                _ = services.AddHostedService<MigrateEfCoreHostService<MyDbContext>>();
            });
}
