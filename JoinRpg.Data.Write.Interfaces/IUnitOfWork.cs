using System.Data.Entity;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;

namespace JoinRpg.Data.Write.Interfaces
{
  public interface IUnitOfWork
  {
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// We must <see cref="DbSet{Entity}"/>, not <see cref="IDbSet{TEntity}"/> because of .Add/RemoveRange() methods</remarks>
    DbSet<T> GetDbSet<T>() where T : class;
    Task SaveChangesAsync();

    IUserRepository GetUsersRepository();
    IProjectRepository GetProjectRepository();
    IClaimsRepository GetClaimsRepository();
  }
}
