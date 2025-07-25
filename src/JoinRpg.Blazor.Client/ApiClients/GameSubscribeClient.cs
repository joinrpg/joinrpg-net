using JoinRpg.Web.ProjectMasterTools.Subscribe;

namespace JoinRpg.Blazor.Client.ApiClients;

public class GameSubscribeClient : IGameSubscribeClient
{
    private readonly HttpClient httpClient;
    private readonly ILogger<GameSubscribeClient> logger;
    private readonly CsrfTokenProvider csrfTokenProvider;

    public GameSubscribeClient(HttpClient httpClient, ILogger<GameSubscribeClient> logger, CsrfTokenProvider csrfTokenProvider)
    {
        this.httpClient = httpClient;
        this.logger = logger;
        this.csrfTokenProvider = csrfTokenProvider;
    }

    public async Task SaveGroupSubscription(int projectId, EditSubscribeViewModel model)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            _ = (await httpClient.PostAsJsonAsync($"webapi/gamesubscribe/save?projectId={projectId}", model)).EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }

    public async Task<SubscribeListViewModel> GetSubscribeForMaster(int projectId, int masterId)
    {
        return await httpClient.GetFromJsonAsync<SubscribeListViewModel>(
            $"webapi/gamesubscribe/getformaster?projectId={projectId}&masterId={masterId}")
            ?? throw new Exception("Couldn't get result from server");
    }

    public async Task RemoveSubscription(int projectId, int userSubscriptionsId)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            _ = (await httpClient.PostAsync($"webapi/gamesubscribe/unsubscribe?projectId={projectId}&userSubscriptionsId={userSubscriptionsId}", null))
                .EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }

    public Task<ClaimSubscribeViewModel> GetSubscribeForClaim(int projectId, int claimId) => throw new NotImplementedException();
}
