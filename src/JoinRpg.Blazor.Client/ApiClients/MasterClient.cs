using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class MasterClient(HttpClient httpClient) : IMasterClient
{
    public async Task<List<UserInfoHeader>> GetMasters(int projectId)
        => await (await httpClient.GetAsync($"webapi/master/GetList?projectId={projectId}"))
            .ReadFromJsonOrThrowProblem<List<UserInfoHeader>>();
}
