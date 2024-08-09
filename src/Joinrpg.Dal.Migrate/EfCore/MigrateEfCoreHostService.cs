using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Joinrpg.Dal.Migrate;

internal class MigrateEfCoreHostService<TContext>(
    IServiceProvider services,
    ILogger<MigrateEfCoreHostService<TContext>> logger,
    IHostApplicationLifetime applicationLifetime) : BackgroundService
    where TContext : DbContext
{
    private async Task MigrateAsync(DbContext dbContext, CancellationToken stoppingToken)
    {
        logger.LogInformation("Start migration of {contextName}", typeof(TContext).FullName);

        var lastAppliedMigration = (await dbContext.Database.GetAppliedMigrationsAsync(stoppingToken)).LastOrDefault();
        if (!string.IsNullOrEmpty(lastAppliedMigration))
        {
            logger.LogInformation("Last applied migration: {LastAppliedMigration}", lastAppliedMigration);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(stoppingToken);
        foreach (var pm in pendingMigrations)
        {
            logger.LogInformation("Pending migration: {PendingMigration}", pm);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        logger.LogInformation("Applying migrations...");
        await dbContext.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Database has been successfully migrated");
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting migrator...");
        await using var scope = services.CreateAsyncScope();
        try
        {
            await MigrateAsync(
                scope.ServiceProvider.GetRequiredService<TContext>(),
                stoppingToken);

            if (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Terminating by cancellation token");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing migrator");
            Environment.ExitCode = 1;
        }
        finally
        {
            applicationLifetime.StopApplication();
        }
    }
}
