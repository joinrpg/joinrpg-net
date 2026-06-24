using JoinRpg.Interfaces;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Integrations.KogdaIgra;

internal class SyncKogdaIgraJob(
    IKogdaIgraSyncService kogdaIgraSyncService,
    ILogger<SyncKogdaIgraJob> logger) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting KogdaIgra synchronization job");

        var status = await kogdaIgraSyncService.GetSyncStatus();
        var beforePending = int.MaxValue;
        do
        {
            beforePending = status.PendingGamesCount;
            cancellationToken.ThrowIfCancellationRequested();
            status = await kogdaIgraSyncService.PerformSync();
        } while (status.PendingGamesCount > 0 && status.PendingGamesCount < beforePending);

        logger.LogInformation("Finished KogdaIgra synchronization job. Pending games: {pendingGamesCount}", status.PendingGamesCount);
    }
}
