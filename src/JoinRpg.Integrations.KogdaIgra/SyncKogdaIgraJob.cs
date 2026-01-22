using JoinRpg.Interfaces;

namespace JoinRpg.Integrations.KogdaIgra;

internal class SyncKogdaIgraJob(IKogdaIgraSyncService kogdaIgraSyncService) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var status = await kogdaIgraSyncService.GetSyncStatus();
        var beforePending = int.MaxValue;
        while (status.PendingGamesCount > 0 && status.PendingGamesCount < beforePending)
        {
            beforePending = status.PendingGamesCount;
            cancellationToken.ThrowIfCancellationRequested();
            status = await kogdaIgraSyncService.PerformSync();
        }
    }
}
