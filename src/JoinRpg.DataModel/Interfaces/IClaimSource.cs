namespace JoinRpg.DataModel;

public interface IClaimSource : IWorldObject
{
    IEnumerable<Claim> Claims { get; }
    bool IsAvailable { get; }
    User ResponsibleMasterUser { get; }
    ICollection<UserSubscription> Subscriptions { get; }
    bool IsRoot { get; }
}
