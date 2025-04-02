using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.Services.Interfaces.Integrations.KogdaIgra;
using JoinRpg.Web.AdminTools.KogdaIgra;
using Microsoft.Extensions.Logging;

namespace JoinRpg.WebPortal.Managers.AdminTools;
internal class KogdaIgraSyncManager(
    IKogdaIgraSyncService kogdaIgraSyncService,
    ILogger<KogdaIgraSyncManager> logger,
    IKogdaIgraRepository kogdaIgraRepository) : IKogdaIgraSyncClient
{
    public async Task<KogdaIgraShortViewModel[]> GetKogdaIgraCandidates()
    {
        var items = await kogdaIgraRepository.GetAll();
        return items.Select(i => new KogdaIgraShortViewModel(i.KogdaIgraId, i.Name)).ToArray();
    }

    public async Task<KogdaIgraCardViewModel> GetKogdaIgraCard(int kogdaIgraId)
    {
        var item = await kogdaIgraRepository.GetById(kogdaIgraId);
        return item.ToViewModel();
    }

    public async Task<SyncStatusViewModel> GetSyncStatus()
    {
        var status = await kogdaIgraSyncService.GetSyncStatus();
        return status.ToViewModel();
    }

    public async Task<ResyncOperationResultsViewModel> ResyncKograIgra()
    {
        var (status, statusMessage) = await PerformSync();
        status ??= (await kogdaIgraSyncService.GetSyncStatus());
        return new ResyncOperationResultsViewModel(statusMessage == null, statusMessage ?? "", status.ToViewModel());

        async Task<(SyncStatus?, string?)> PerformSync()
        {
            try
            {
                return (await kogdaIgraSyncService.PerformSync(), null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during sync with kogda-igra");
                return (null, $"Error during sync: {ex.Message}");
            }
        }
    }
}
