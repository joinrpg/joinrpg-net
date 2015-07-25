using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories
{
  [UsedImplicitly]
  public class ClaimsRepositoryImpl : RepositoryImplBase, IClaimsRepository
  {
    public IEnumerable<Claim> GetClaimsForUser(int userId)
    {
      return Ctx.ClaimSet.Where(claim => claim.PlayerUserId == userId);
    }

    public ClaimsRepositoryImpl(MyDbContext ctx) : base(ctx)
    {
    }
  }
}
