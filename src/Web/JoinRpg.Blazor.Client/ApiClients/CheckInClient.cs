using JoinRpg.Web.CheckIn;

namespace JoinRpg.Blazor.Client.ApiClients;

public class CheckInClient(HttpClient httpClient) : ICheckInClient
{
    public async Task<CheckInStatViewModel> GetCheckInStats(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/checkin/getstats?projectId={projectId.Value}"))
            .ReadFromJsonOrThrowProblem<CheckInStatViewModel>();
}
