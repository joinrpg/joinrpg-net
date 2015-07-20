using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories
{
  [UsedImplicitly]
  public class UserInfoRepository : IUserRepository
  {
    private readonly MyDbContext _ctx;

    public UserInfoRepository(MyDbContext ctx)
    {
      _ctx = ctx;
    }

    public User GetById(int id) => _ctx.UserSet.Find(id);
  }
}
