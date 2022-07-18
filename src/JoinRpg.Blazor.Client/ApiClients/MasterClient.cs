using System.Net.Http.Json;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class MasterClient : IMasterClient
{
    private readonly HttpClient httpClient;

    public MasterClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }
    public async Task<List<MasterViewModel>> GetMasters(int projectId)
    {
        return await httpClient.GetFromJsonAsync<List<MasterViewModel>>(
             $"webapi/master/GetList?projectId={projectId}")
             ?? throw new Exception("Couldn't get result from server");
    }
}
