namespace JoinRpg.Web.GameSubscribe;

public interface IGameSubscribeClient
{
    Task<SubscribeListViewModel> GetSubscribeForMaster(int projectId, int masterId);
    Task RemoveSubscription(int projectId, int userSubscriptionsId);
}
