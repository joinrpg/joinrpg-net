using JoinRpg.Web.CheckIn;

namespace JoinRpg.Blazor.Client.ApiClients;

public class CheckInClient(HttpClient httpClient) : ICheckInClient
{
    public async Task<CheckInStatViewModel> GetCheckInStats(ProjectIdentification projectId)
    {
        return await httpClient.GetFromJsonAsync<CheckInStatViewModel>(
            $"webapi/checkin/getstats?projectId={projectId.Value}")
            ?? throw new Exception("Couldn't get result from server");
    }
}
