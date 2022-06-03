using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Joinrpg.Dal.Migrate;

internal class MigrateEfCoreHostService<TContext> : Microsoft.Extensions.Hosting.BackgroundService
    where TContext : DbContext
{
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IServiceProvider _services;

    public MigrateEfCoreHostService(
        IServiceProvider services,
        ILogger<MigrateEfCoreHostService<TContext>> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _applicationLifetime = applicationLifetime;
        _services = services;
    }

    private async Task MigrateAsync(DbContext dbContext, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Start migration of {contextName}", typeof(TContext).FullName);

        var lastAppliedMigration = (await dbContext.Database.GetAppliedMigrationsAsync(stoppingToken)).LastOrDefault();
        if (!string.IsNullOrEmpty(lastAppliedMigration))
        {
            _logger.LogInformation("Last applied migration: {LastAppliedMigration}", lastAppliedMigration);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(stoppingToken);
        foreach (var pm in pendingMigrations)
        {
            _logger.LogInformation("Pending migration: {PendingMigration}", pm);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        _logger.LogInformation("Applying migrations...");
        await dbContext.Database.MigrateAsync(stoppingToken);
        _logger.LogInformation("Database has been successfully migrated");
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting migrator...");
        await using var scope = _services.CreateAsyncScope();
        try
        {
            await MigrateAsync(
                scope.ServiceProvider.GetRequiredService<TContext>(),
                stoppingToken);

            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Terminating by cancellation token");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing migrator");
            Environment.ExitCode = 1;
        }
        finally
        {
            _applicationLifetime.StopApplication();
        }
    }
}
