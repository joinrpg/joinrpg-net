namespace JoinRpg.Services.Interfaces.Subscribe;

public interface IGameSubscribeService
{
    Task UpdateSubscribeForGroup(SubscribeForGroupRequest request);
    Task RemoveSubscribe(RemoveSubscribeRequest request);
    Task SubscribeClaimToUser(ClaimIdentification claimId);
    Task UnsubscribeClaimToUser(ClaimIdentification claimId);
}
