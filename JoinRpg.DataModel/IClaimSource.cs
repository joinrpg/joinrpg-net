using System.Collections.Generic;

namespace JoinRpg.DataModel
{
  public interface IClaimSource : IWorldObject
  {
    ICollection<Claim> Claims { get; set; }
    bool IsAvailable { get; }
  }
}