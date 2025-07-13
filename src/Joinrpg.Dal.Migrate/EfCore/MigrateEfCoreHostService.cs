using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Dal.Migrate.EfCore;

internal class MigrateEfCoreHostService<TContext>(
    TContext dbContext,
    ILogger<MigrateEfCoreHostService<TContext>> logger) : IMigratorService
    where TContext : DbContext
{
    private static readonly string contextName = typeof(TContext).Name;

    public async Task MigrateAsync(CancellationToken ct)
    {
        if (dbContext.Database.HasPendingModelChanges())
        {
            throw new InvalidOperationException($"There are pending changes in model {contextName}");
        }

        var lastAppliedMigration = (await dbContext.Database.GetAppliedMigrationsAsync(ct)).LastOrDefault();
        if (lastAppliedMigration is null)
        {
            logger.LogWarning("There are no applied migrations or primary database connection failed.");
        }
        else
        {
            logger.LogInformation(
                "Last applied migration for the {DbContextName} database is {MigrationName}",
                contextName,
                lastAppliedMigration);
        }

        ct.ThrowIfCancellationRequested();

        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(ct)).ToArray();
        if (pendingMigrations.Length > 0)
        {
            logger.LogInformation("Pending migrations are:\n{PendingMigrations}", string.Join("\n", pendingMigrations));
            await dbContext.Database.MigrateAsync(ct);
            logger.LogInformation("Database {dbContext} has been successfully migrated.", contextName);
        }
        else
        {
            logger.LogInformation("No migrations were applied. The database is already up to date.");
        }
    }
}
