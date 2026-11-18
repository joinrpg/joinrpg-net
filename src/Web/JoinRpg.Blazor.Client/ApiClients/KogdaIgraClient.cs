using JoinRpg.Web.ProjectCommon.KogdaIgra;

namespace JoinRpg.Blazor.Client.ApiClients;

public class KogdaIgraClient(HttpClient httpClient, ILogger<KogdaIgraClient> logger, CsrfTokenProvider csrfTokenProvider)
    : IKogdaIgraSyncClient, IKogdaIgraBindClient
{
    public async Task<KogdaIgraShortViewModel[]> GetKogdaIgraCandidates()
        => await (await httpClient.GetAsync("webapi/kogdaigra/GetKogdaIgraCandidates"))
            .ReadFromJsonOrThrowProblem<KogdaIgraShortViewModel[]>();

    public async Task<KogdaIgraShortViewModel[]> GetFutureKogdaIgraCandidates()
        => await (await httpClient.GetAsync("webapi/kogdaigra/GetFutureKogdaIgraCandidates"))
            .ReadFromJsonOrThrowProblem<KogdaIgraShortViewModel[]>();

    public async Task<KogdaIgraCardViewModel[]> GetKogdaIgraCards(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIds)
        => await (await httpClient.GetAsync($"webapi/kogdaigra/GetKogdaIgraCard?kogdaIgraId={string.Join(",", kogdaIgraIds.Select(x => x.ToString()))}"))
            .ReadFromJsonOrThrowProblem<KogdaIgraCardViewModel[]>();

    public async Task<KogdaIgraShortViewModel[]> GetKogdaIgraNotUpdated()
        => await (await httpClient.GetAsync("webapi/kogdaigra/GetKogdaIgraNotUpdated"))
            .ReadFromJsonOrThrowProblem<KogdaIgraShortViewModel[]>();

    public async Task<SyncStatusViewModel> GetSyncStatus()
        => await (await httpClient.GetAsync("webapi/kogdaigra/GetSyncStatus"))
            .ReadFromJsonOrThrowProblem<SyncStatusViewModel>();

    public async Task<ResyncOperationResultsViewModel> ResyncKograIgra()
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsync($"webapi/kogdaigra/Resync", content: null);
            return await response.ReadFromJsonOrThrowProblem<ResyncOperationResultsViewModel>();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during access");
            try
            {
                var status = await GetSyncStatus();
                return new ResyncOperationResultsViewModel(false, e.Message, status);
            }
            catch
            {
                return new ResyncOperationResultsViewModel(false, e.Message, new SyncStatusViewModel(0, DateTimeOffset.UnixEpoch, 0)); ;
            }
        }
    }

    public async Task<ResyncOperationResultsViewModel> ForceResyncGames(KogdaIgraIdentification[] gameIds)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsJsonAsync("webapi/kogdaigra/ForceResync", gameIds);
            return await response.ReadFromJsonOrThrowProblem<ResyncOperationResultsViewModel>();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during force resync");
            try
            {
                var status = await GetSyncStatus();
                return new ResyncOperationResultsViewModel(false, e.Message, status);
            }
            catch
            {
                return new ResyncOperationResultsViewModel(false, e.Message, new SyncStatusViewModel(0, DateTimeOffset.UnixEpoch, 0));
            }
        }
    }

    public async Task<ResyncOperationResultsViewModel> ScheduleUpdateAllGames()
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsync($"webapi/kogdaigra/ScheduleUpdateAll", content: null);
            return await response.ReadFromJsonOrThrowProblem<ResyncOperationResultsViewModel>();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during scheduling update for all games");
            try
            {
                var status = await GetSyncStatus();
                return new ResyncOperationResultsViewModel(false, e.Message, status);
            }
            catch
            {
                return new ResyncOperationResultsViewModel(false, e.Message, new SyncStatusViewModel(0, DateTimeOffset.UnixEpoch, 0));
            }
        }
    }

    public async Task<ResyncOperationResultsViewModel> RunSyncJob()
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsync($"webapi/kogdaigra/RunSyncJob", content: null);
            return await response.ReadFromJsonOrThrowProblem<ResyncOperationResultsViewModel>();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during running sync job");
            try
            {
                var status = await GetSyncStatus();
                return new ResyncOperationResultsViewModel(false, e.Message, status);
            }
            catch
            {
                return new ResyncOperationResultsViewModel(false, e.Message, new SyncStatusViewModel(0, DateTimeOffset.UnixEpoch, 0));
            }
        }
    }

    public async Task UpdateProjectKogdaIgraBindings(KogdaIgraBindViewModel command)

    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsJsonAsync($"webapi/kogdaigra/UpdateBindings", command);

        await response.EnsureSuccessOrThrowProblem();
    }

}
