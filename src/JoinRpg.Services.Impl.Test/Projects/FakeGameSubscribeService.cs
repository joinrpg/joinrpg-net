using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Subscribe;

namespace JoinRpg.Services.Impl.Test.Projects;

/// <summary>Записывает вызовы <see cref="IGameSubscribeService.RemoveAllSubscriptions"/>.</summary>
internal sealed class FakeGameSubscribeService : IGameSubscribeService
{
    public List<(ProjectIdentification ProjectId, UserIdentification UserId)> RemoveAllSubscriptionsCalls { get; } = [];

    public Task RemoveAllSubscriptions(ProjectIdentification projectId, UserIdentification userId)
    {
        RemoveAllSubscriptionsCalls.Add((projectId, userId));
        return Task.CompletedTask;
    }

    public Task UpdateSubscribeForGroup(SubscribeForGroupRequest request) => throw new NotSupportedException();
    public Task RemoveSubscribe(RemoveSubscribeRequest request) => throw new NotSupportedException();
    public Task SubscribeClaimToUser(ClaimIdentification claimId) => throw new NotSupportedException();
    public Task UnsubscribeClaimToUser(ClaimIdentification claimId) => throw new NotSupportedException();
}
