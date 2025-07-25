using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class MasterClient(HttpClient httpClient) : IMasterClient
{
    public async Task<List<MasterViewModel>> GetMasters(int projectId)
    {
        return await httpClient.GetFromJsonAsync<List<MasterViewModel>>(
             $"webapi/master/GetList?projectId={projectId}")
             ?? throw new Exception("Couldn't get result from server");
    }
}
