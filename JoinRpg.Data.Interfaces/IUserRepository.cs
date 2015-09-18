using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
   public interface IUserRepository
   {
     User GetById(int id);

     Task<User> WithProfile(int userId);
   }
}
