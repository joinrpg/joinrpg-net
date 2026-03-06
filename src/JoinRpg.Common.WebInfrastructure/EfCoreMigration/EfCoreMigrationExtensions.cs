using Microsoft.EntityFrameworkCore;

namespace JoinRpg.Common.WebInfrastructure.EfCoreMigration;

public static class EfCoreMigrationExtensions
{
    public static void RegisterMigrator<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string connectionStringName,
        Action<DbContextOptionsBuilder>? optionsBuilder = null)
        where TContext : DbContext
    {
        _ = services
            .AddScoped<IMigratorService, MigrateEfCoreHostService<TContext>>()
            .AddJoinEfCoreDbContext<TContext>(configuration, environment, connectionStringName, optionsBuilder);
    }
}
