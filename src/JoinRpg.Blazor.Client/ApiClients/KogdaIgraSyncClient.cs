using System.Net.Http.Json;
using JoinRpg.Web.AdminTools.KogdaIgra;

namespace JoinRpg.Blazor.Client.ApiClients;

public class KogdaIgraSyncClient(HttpClient httpClient, ILogger<KogdaIgraSyncClient> logger, CsrfTokenProvider csrfTokenProvider)
    : IKogdaIgraSyncClient
{
    public async Task<KogdaIgraShortViewModel[]> GetKogdaIgraCandidates()
    {
        return await httpClient.GetFromJsonAsync<KogdaIgraShortViewModel[]>("webapi/kogdaigra/GetKogdaIgraCandidates")
            ?? throw new Exception("Couldn't get result from server");
    }
    public async Task<KogdaIgraCardViewModel> GetKogdaIgraCard(int kogdaIgraId)
    {
        return await httpClient.GetFromJsonAsync<KogdaIgraCardViewModel>($"webapi/kogdaigra/GetKogdaIgraCard?kogdaIgraId={kogdaIgraId}")
            ?? throw new Exception("Couldn't get result from server");
    }

    public async Task<KogdaIgraShortViewModel[]> GetKogdaIgraNotUpdated()
    {
        return await httpClient.GetFromJsonAsync<KogdaIgraShortViewModel[]>("webapi/kogdaigra/GetKogdaIgraNotUpdated")
           ?? throw new Exception("Couldn't get result from server");
    }

    public async Task<SyncStatusViewModel> GetSyncStatus()
    {
        return await httpClient.GetFromJsonAsync<SyncStatusViewModel>("webapi/kogdaigra/GetSyncStatus")
            ?? throw new Exception("Couldn't get result from server");
    }

    public async Task<ResyncOperationResultsViewModel> ResyncKograIgra()
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            var response = await httpClient.PostAsync($"webapi/kogdaigra/Resync", content: null);

            return await response
                .EnsureSuccessStatusCode()
                .Content
                .ReadFromJsonAsync<ResyncOperationResultsViewModel>()
                ?? throw new Exception("Empty");
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
}
