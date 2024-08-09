using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Joinrpg.Dal.Migrate;

internal class MigrateEfCoreHostService<TContext>(
    TContext dbContext,
    ILogger<MigrateEfCoreHostService<TContext>> logger) : IMigratorService
    where TContext : DbContext
{
    private static readonly string contextName = typeof(TContext).Name!;
    public async Task MigrateAsync(CancellationToken stoppingToken)
    {
        using var logScope = logger.BeginScope("Migration of {dbContext}", contextName);

        logger.LogInformation("Start migration of {dbContext}", contextName);

        var lastAppliedMigration = (await dbContext.Database.GetAppliedMigrationsAsync(stoppingToken)).LastOrDefault();
        if (!string.IsNullOrEmpty(lastAppliedMigration))
        {
            logger.LogInformation("Last applied migration for {dbContext}: {LastAppliedMigration}", contextName, lastAppliedMigration);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        if (dbContext.Database.HasPendingModelChanges())
        {
            logger.LogError("There is pending changes in model!");
            throw new InvalidOperationException("Pending changes in model");
        }

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(stoppingToken);

        foreach (var pm in pendingMigrations)
        {
            logger.LogInformation("Pending migration for {dbContext}: {PendingMigration}", contextName, pm);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        logger.LogInformation("Applying migrations for {dbContext} ...", contextName);
        await dbContext.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Database {dbContext} has been successfully migrated", contextName);
    }
}
