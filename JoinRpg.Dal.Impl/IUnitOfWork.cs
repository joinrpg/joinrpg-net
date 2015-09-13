using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.Dal.Impl
{
  public interface IUnitOfWork
  {
    DbSet<T> GetDbSet<T>() where T : class;
    void SaveChanges();
    Task SaveChangesAsync();
  }
}
