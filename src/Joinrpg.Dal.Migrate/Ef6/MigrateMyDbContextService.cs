using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.SqlClient;
using JoinRpg.Common.WebInfrastructure.EfCoreMigration;
using JoinRpg.Dal.Impl;
using JoinRpg.Dal.Impl.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Dal.Migrate.Ef6;

internal class MigrateMyDbContextService(
    ILogger<MigrateMyDbContextService> logger,
    IConfiguration configuration)
    : IMigratorService
{
    // SQL Server: «Cannot open database requested by the login» — базы ещё нет, Update() её создаст.
    private const int CannotOpenDatabaseErrorNumber = 4060;

    public string Name => nameof(MyDbContext);

    private string ConnectionString => configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("There is no connection string found for the primary database");

    public async Task CheckConnectionAsync(CancellationToken ct)
    {
        // Сам DbMigrator при недоступной базе не падает, а отдаёт пустой список миграций,
        // поэтому подключение проверяем напрямую.
        await using var connection = new SqlConnection(ConnectionString);
        try
        {
            await connection.OpenAsync(ct);
        }
        catch (SqlException ex) when (ex.Number == CannotOpenDatabaseErrorNumber)
        {
            logger.LogWarning("The primary database does not exist yet, it will be created.");
        }
    }

    public Task MigrateAsync(CancellationToken ct)
    {
        logger.LogInformation("Creating migrator for the primary database.");

        var connectionString = ConnectionString;

        // TODO mask connection string from logs;
        // logger.LogInformation("Discovered connection string {connectionString}", connectionString);

        // Конфигурация та же, что у тестов и EF6 CLI: в ней Seed, создающий виртуальных пользователей.
        var config = new Configuration
        {
            TargetDatabase = new DbConnectionInfo(connectionString, "System.Data.SqlClient"),
            CommandTimeout = 900,  // seconds
        };
        var migrator = new MigratorLoggingDecorator(new DbMigrator(config), new MigrationsLoggerILoggerAdapter(logger));
        logger.LogInformation("Migrator created.");

        ct.ThrowIfCancellationRequested();

        var lastLocalMigration = GetLastMigration(migrator.GetLocalMigrations());
        if (lastLocalMigration is null)
        {
            throw new InvalidOperationException("No migrations for the primary database found.");
        }

        logger.LogInformation("Last designed migration for the primary database is {MigrationName}", lastLocalMigration);

        ct.ThrowIfCancellationRequested();

        var lastAppliedMigration = GetLastMigration(migrator.GetDatabaseMigrations());
        if (lastAppliedMigration is null)
        {
            logger.LogWarning("There are no applied migrations or primary database connection failed.");
        }
        else
        {
            logger.LogInformation("Last applied migration for the primary database is {MigrationName}", lastAppliedMigration);
        }

        ct.ThrowIfCancellationRequested();

        var pendingMigrations = migrator.GetPendingMigrations().ToArray();
        if (pendingMigrations.Length > 0)
        {
            logger.LogInformation("Pending migrations are:\n{PendingMigrations}", string.Join("\n", pendingMigrations));
        }
        else
        {
            logger.LogInformation("No pending migrations. The database schema is already up to date.");
        }

        // Update() зовём и без новых миграций: он же запускает идемпотентный Seed,
        // так что виртуальные пользователи появятся и в базе, мигрированной раньше без них.
        migrator.Update(); // TODO: pass migration name from command line to allow reverts
        logger.LogInformation("The primary database has been successfully migrated and seeded.");

        return Task.CompletedTask;
    }

    private static string? GetLastMigration(IEnumerable<string> migrations) => migrations.OrderBy(x => x).LastOrDefault();
}
