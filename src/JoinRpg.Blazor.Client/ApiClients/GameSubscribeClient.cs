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
            await (await httpClient.PostAsJsonAsync($"webapi/gamesubscribe/save?projectId={projectId}", model)).EnsureSuccessOrThrowProblem();
        }
        catch (Exception e) when (e is not ServerProblemException)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }

    public async Task<SubscribeListViewModel> GetSubscribeForMaster(int projectId, int masterId)
    {
        return await (await httpClient.GetAsync($"webapi/gamesubscribe/getformaster?projectId={projectId}&masterId={masterId}"))
            .ReadFromJsonOrThrowProblem<SubscribeListViewModel>();
    }

    public async Task RemoveSubscription(int projectId, int userSubscriptionsId)
    {
        try
        {
            await csrfTokenProvider.SetCsrfToken(httpClient);
            await (await httpClient.PostAsync($"webapi/gamesubscribe/unsubscribe?projectId={projectId}&userSubscriptionsId={userSubscriptionsId}", null))
                .EnsureSuccessOrThrowProblem();
        }
        catch (Exception e) when (e is not ServerProblemException)
        {
            logger.LogError(e, "Error during access");
            throw;
        }
    }

    public async Task<ClaimSubscribeViewModel> GetSubscribeForClaim(ClaimIdentification claimId)
    {
        return await (await httpClient.GetAsync(
            $"webapi/gamesubscribe/getforclaim?projectId={claimId.ProjectId.Value}&claimId={claimId.ClaimId}"))
            .ReadFromJsonOrThrowProblem<ClaimSubscribeViewModel>();
    }

    public async Task<ClaimSubscribeViewModel> SubscribeClaimToUser(ClaimIdentification claimId)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        return await (await httpClient.PostAsync(
            $"webapi/gamesubscribe/subscribeclaim?projectId={claimId.ProjectId.Value}&claimId={claimId.ClaimId}", null))
            .ReadFromJsonOrThrowProblem<ClaimSubscribeViewModel>();
    }

    public async Task<ClaimSubscribeViewModel> UnsubscribeClaimToUser(ClaimIdentification claimId)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        return await (await httpClient.PostAsync(
            $"webapi/gamesubscribe/unsubscribeclaim?projectId={claimId.ProjectId.Value}&claimId={claimId.ClaimId}", null))
            .ReadFromJsonOrThrowProblem<ClaimSubscribeViewModel>();
    }
}
