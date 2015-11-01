using System.Collections.Generic;

namespace JoinRpg.DataModel
{
  public interface IClaimSource : IWorldObject
  {
    ICollection<Claim> Claims { get; }
    bool  IsAvailable { get; }
    User ResponsibleMasterUser { get; }
    ICollection<UserSubscription>  Subscriptions { get; }
  }
}