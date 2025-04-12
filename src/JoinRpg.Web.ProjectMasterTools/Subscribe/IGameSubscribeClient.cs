namespace JoinRpg.Web.ProjectMasterTools.Subscribe;

public interface IGameSubscribeClient
{
    Task<SubscribeListViewModel> GetSubscribeForMaster(int projectId, int masterId);
    Task RemoveSubscription(int projectId, int userSubscriptionsId);

    Task SaveGroupSubscription(int projectId, EditSubscribeViewModel model);

    Task<ClaimSubscribeViewModel> GetSubscribeForClaim(int projectId, int claimId);
}
