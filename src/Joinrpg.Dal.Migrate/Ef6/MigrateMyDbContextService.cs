using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Joinrpg.Dal.Migrate.Ef6;

internal class MigrateMyDbContextService(
    ILogger<MigrateMyDbContextService> logger,
    IConfiguration configuration)
    : IMigratorService
{
    public Task MigrateAsync(CancellationToken ct)
    {
        logger.LogInformation("Create migration");

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new Exception("There is no connection string");

        //TODO mask connection string from logs;
        //logger.LogInformation("Discovered connection string {connectionString}", connectionString);

        var migrator = new MigratorLoggingDecorator(new DbMigrator(new JoinMigrationsConfig(connectionString)), new MigrationsLoggerILoggerAdapter(logger));
        logger.LogInformation("Migrator created");

        logger.LogInformation("Start migration");

        logger.LogInformation("Last local migration {lastLocal}", migrator.GetLocalMigrations().OrderBy(x => x).LastOrDefault());
        logger.LogInformation("Last DB migration {lastDb}", migrator.GetDatabaseMigrations().OrderBy(x => x).LastOrDefault());

        var pending = migrator.GetPendingMigrations();
        logger.LogInformation("Pending migrations {pending}", string.Join("\n", pending));
        migrator.Update(); // TODO pass migration name from command line to allow reverts
        logger.LogInformation("Migration completed");

        return Task.CompletedTask;
    }
}
