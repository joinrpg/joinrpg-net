using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces.Subscribe;

/// <summary>
/// Load user subscribe settings 
/// </summary>
public interface IUserSubscribeRepository
{
    /// <summary>
    /// Load every Subscriptions for project
    /// </summary>
    Task<(User User, UserSubscriptionDto[] UserSubscriptions)> LoadSubscriptionsForProject(UserIdentification userId, ProjectIdentification projectId);

    /// <summary>
    /// Load subscribtion by id
    /// </summary>
    Task<UserSubscriptionDto> LoadSubscriptionById(ProjectIdentification projectId, int subscriptionId);
    Task<IReadOnlyCollection<UserSubscribe>> GetDirect(IReadOnlyCollection<ClaimIdentification> claimId);
    Task<IReadOnlyCollection<UserSubscribe>> GetForCharAndGroups(
        IReadOnlyCollection<CharacterGroupIdentification> characterGroupIdentifications,
        IReadOnlyCollection<CharacterIdentification> characterId);
}
