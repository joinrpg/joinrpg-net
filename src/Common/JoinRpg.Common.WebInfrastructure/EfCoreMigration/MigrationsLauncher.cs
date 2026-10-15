using Microsoft.Extensions.Logging;

namespace JoinRpg.Common.WebInfrastructure.EfCoreMigration;

internal class MigrationsLauncher(
    IHostApplicationLifetime applicationLifetime,
    ILogger<MigrationsLauncher> logger,
    IServiceProvider services) : IHostedService
{
    private const int MigrationInterruptedErrorCode = 1;

    private readonly CancellationTokenSource migrationCancellationTokenSource = new();
    private readonly TaskCompletionSource migrationCompletionTaskSource = new();

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        applicationLifetime.ApplicationStarted.Register(StartExecution);
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        migrationCancellationTokenSource.Cancel();
        // Defer completion promise, until our application has reported it is done.
        return migrationCompletionTaskSource.Task; //Should be non-null after Start
    }

    private void StartExecution()
    {
        try
        {
            Task.Run(() => ExecuteMigrators(migrationCancellationTokenSource.Token), applicationLifetime.ApplicationStopping);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while starting launcher");
            migrationCompletionTaskSource.SetResult();
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
            var migrators = scope.ServiceProvider.GetServices<IMigratorService>().ToArray();

            // Сначала проверяем все базы и только потом мигрируем: иначе ошибка в строке подключения
            // к последней базе всплывает, когда первые уже мигрированы.
            foreach (var migrator in migrators)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await RunMigratorStep(migrator, "Checking connection", migrator.CheckConnectionAsync, cancellationToken);
            }

            foreach (var migrator in migrators)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await RunMigratorStep(migrator, "Migrating", migrator.MigrateAsync, cancellationToken);
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
            migrationCompletionTaskSource.SetResult();
            applicationLifetime.StopApplication();
        }
    }

    private async Task RunMigratorStep(
        IMigratorService migrator,
        string stepName,
        Func<CancellationToken, Task> step,
        CancellationToken cancellationToken)
    {
        try
        {
            using var loggingScope = logger.BeginScope("Migration of {migrator}", migrator.Name);
            logger.LogInformation("{MigratorStep} {MigratorName}", stepName, migrator.Name);
            await step(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "{MigratorStep} {MigratorName} failed", stepName, migrator.Name);
            Environment.ExitCode = MigrationInterruptedErrorCode;
            throw;
        }
    }
}
