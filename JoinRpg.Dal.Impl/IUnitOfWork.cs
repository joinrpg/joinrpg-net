using System.Data.Entity;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;

namespace JoinRpg.Dal.Impl
{
  public interface IUnitOfWork
  {
    DbSet<T> GetDbSet<T>() where T : class;
    void SaveChanges();
    Task SaveChangesAsync();

    IUserRepository GetUsersRepository();
    IProjectRepository GetProjectRepository();
    IClaimsRepository GetClaimsRepository();
  }
}
