using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using System.Data.Entity;

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
    public Task<User> WithProfile(int userId)
    {
      return _ctx.Set<User>()
        .Include(u => u.Auth)
        .Include(u => u.Allrpg)
        .Include(u => u.Extra)
        .SingleOrDefaultAsync(u => u.UserId == userId);
    }
  }
}
