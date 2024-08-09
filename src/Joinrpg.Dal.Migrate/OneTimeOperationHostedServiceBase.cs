using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Joinrpg.Dal.Migrate;

internal class OneTimeOperationHostedServiceBase(
    IHostApplicationLifetime applicationLifetime,
    ILogger<OneTimeOperationHostedServiceBase> logger,
    IEnumerable<IMigratorService> migrators) : IHostedService
{
    private CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

    private Task? task;

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting task");
        task = Task.Run(async () =>
        {
            try
            {
                foreach (var migrator in migrators)
                {
                    using var scope = logger.BeginScope("Migration of {migrator}", migrator.GetType().Name);
                    await migrator.MigrateAsync(cancellationToken);
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
        }, cancellationToken);
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource.Cancel();
        // Defer completion promise, until our application has reported it is done.
        return task!; //Should be non-null after Start
    }
}
