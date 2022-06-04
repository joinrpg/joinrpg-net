using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Joinrpg.Dal.Migrate;

internal abstract class OneTimeOperationHostedServiceBase : IHostedService
{
    private CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

    private Task? task;
    private readonly IHostApplicationLifetime applicationLifetime;
    protected readonly ILogger logger;

    public OneTimeOperationHostedServiceBase(IHostApplicationLifetime applicationLifetime, ILogger<MigrateHostService> logger)
    {
        this.applicationLifetime = applicationLifetime;
        this.logger = logger;
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting task");
        task = Task.Run(() =>
        {
            try
            {
                DoWork();
            }catch (Exception ex)
            {
                logger.LogError(ex, "Error executing migrator");
                Environment.ExitCode = 1;
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
        }, CancellationToken.None);
        return Task.CompletedTask;
    }

    internal abstract void DoWork();

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource.Cancel();
        // Defer completion promise, until our application has reported it is done.
        return task!; //Should be non-null after Start
    }
}
