using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Joinrpg.Dal.Migrate.EfCore;
internal static class EfCoreMigrationExtensions
{
    internal static void RegisterMigrator<TContext>(this IServiceCollection services, string connectionString)
        where TContext : DbContext
    {
        _ = services.AddScoped<IMigratorService, MigrateEfCoreHostService<TContext>>();
        _ = services.AddDbContext<TContext>(options =>
            {
                _ = options
                .UseNpgsql(connectionString)
                .EnableSensitiveDataLogging(false) // Logs of migration is publicly accessible
                .EnableDetailedErrors(true); // This will be helpful
            });
    }
}
