using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Dal.Migrate;

internal class MigrationsLauncher(
    IHostApplicationLifetime applicationLifetime,
    ILogger<MigrationsLauncher> logger,
    IServiceProvider services) : IHostedService
{
    private const int MigrationInterruptedErrorCode = 1;

    private readonly CancellationTokenSource _migrationCancellationTokenSource = new();
    private readonly TaskCompletionSource _migrationCompletionTaskSource = new();

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        applicationLifetime.ApplicationStarted.Register(StartExecution);
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        _migrationCancellationTokenSource.Cancel();
        // Defer completion promise, until our application has reported it is done.
        return _migrationCompletionTaskSource.Task; //Should be non-null after Start
    }

    private void StartExecution()
    {
        try
        {
            Task.Run(() => ExecuteMigrators(_migrationCancellationTokenSource.Token), applicationLifetime.ApplicationStopping);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while starting launcher");
            _migrationCompletionTaskSource.SetResult();
            Environment.ExitCode = MigrationInterruptedErrorCode;
            applicationLifetime.StopApplication();
        }
    }

    private async Task ExecuteMigrators(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting launcher.");

        await using var scope = services.CreateAsyncScope();

        try
        {
            var migrators = scope.ServiceProvider.GetServices<IMigratorService>();
            foreach (var migrator in migrators)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ExecuteMigrator(migrator, migrator.GetType().Name, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogCritical("Migration process has been cancelled.");
            Environment.ExitCode = MigrationInterruptedErrorCode;
        }
        catch (Exception ex) when (Environment.ExitCode != MigrationInterruptedErrorCode)
        {
            logger.LogError(ex, "Error while executing migrators");
            Environment.ExitCode = MigrationInterruptedErrorCode;
        }
        finally
        {
            _migrationCompletionTaskSource.SetResult();
            applicationLifetime.StopApplication();
        }
    }

    private async Task ExecuteMigrator(
        IMigratorService migrator,
        string migratorName,
        CancellationToken cancellationToken)
    {
        try
        {
            using var loggingScope = logger.BeginScope("Migration of {migrator}", migratorName);
            logger.LogInformation("Launching migrator {MigratorName}", migratorName);
            await migrator.MigrateAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error executing migrator {MigratorName}", migratorName);
            Environment.ExitCode = MigrationInterruptedErrorCode;
            throw;
        }
    }
}
