using Joinrpg.Dal.Migrate.EfCore;
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
                _ = services.AddHostedService<MigrateHostService>();

                services.RegisterMigrator<DataProtectionDbContext>(hostContext.Configuration.GetConnectionString("DataProtection"));
            });
}
