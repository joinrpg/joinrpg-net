using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class MasterClient(HttpClient httpClient) : IMasterClient
{
    public async Task<List<UserInfoHeader>> GetMasters(int projectId)
    {
        return await httpClient.GetFromJsonAsync<List<UserInfoHeader>>(
             $"webapi/master/GetList?projectId={projectId}")
             ?? throw new Exception("Couldn't get result from server");
    }
}
