using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Joinrpg.Dal.Migrate.EfCore;
internal static class EfCoreMigrationExtensions
{
    internal static void RegisterMigrator<TContext>(this IServiceCollection services, string connectionString)
        where TContext : DbContext
    {
        _ = services.AddHostedService<MigrateEfCoreHostService<TContext>>();
        _ = services.AddDbContext<TContext>(options =>
            {
                options.UseNpgsql(connectionString);
                options.EnableSensitiveDataLogging(false); // Logs of migration is publicly accessible
                options.EnableDetailedErrors(true); // This will be helpful
            });
    }
}
