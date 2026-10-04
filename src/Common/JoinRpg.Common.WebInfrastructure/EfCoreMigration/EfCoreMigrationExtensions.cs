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
        // Без строки подключения DbContext не регистрируется, и мигратор падал бы при запуске
        // невнятной ошибкой DI («Unable to resolve service for type ...DbContext»).
        if (!services.AddJoinEfCoreDbContext<TContext>(configuration, environment, connectionStringName, optionsBuilder))
        {
            throw new InvalidOperationException(
                $"Не задана строка подключения ConnectionStrings:{connectionStringName} для {typeof(TContext).Name}");
        }
        _ = services.AddScoped<IMigratorService, MigrateEfCoreHostService<TContext>>();
    }

    public static IServiceCollection AddMigrationsLauncher(this IServiceCollection services) => services.AddHostedService<MigrationsLauncher>();
}
