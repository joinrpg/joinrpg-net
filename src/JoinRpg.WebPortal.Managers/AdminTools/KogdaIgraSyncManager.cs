using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.Services.Interfaces.Integrations.KogdaIgra;
using JoinRpg.Web.AdminTools.KogdaIgra;

namespace JoinRpg.WebPortal.Managers.AdminTools;
internal class KogdaIgraSyncManager(
    IKogdaIgraSyncService kogdaIgraSyncService,
    ILogger<KogdaIgraSyncManager> logger,
    IKogdaIgraRepository kogdaIgraRepository,
    IOptions<KogdaIgraOptions> kograIgraOptions) : IKogdaIgraSyncClient
{
    public async Task<KogdaIgraShortViewModel[]> GetKogdaIgraCandidates()
    {
        var items = await kogdaIgraRepository.GetActive();
        return ToShortViewModels(items);
    }

    private KogdaIgraShortViewModel[] ToShortViewModels((int KogdaIgraId, string Name)[] items)
        => items.Select(i => new KogdaIgraShortViewModel(i.KogdaIgraId, i.Name, new Uri(kograIgraOptions.Value.HostName + "game/" + i.KogdaIgraId))).ToArray();
    public async Task<KogdaIgraCardViewModel> GetKogdaIgraCard(int kogdaIgraId)
    {
        var item = await kogdaIgraRepository.GetById(kogdaIgraId);
        return item.ToViewModel();
    }

    public async Task<KogdaIgraShortViewModel[]> GetKogdaIgraNotUpdated()
    {
        var items = await kogdaIgraRepository.GetNotUpdated();
        return ToShortViewModels(items);
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
