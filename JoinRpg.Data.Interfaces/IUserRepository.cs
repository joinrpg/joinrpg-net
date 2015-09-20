using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
   public interface IUserRepository
   {
     User GetById(int id);

     Task<User> WithProfile(int userId);
     Task<User> GetWithSubscribe(int currentUserId);
   }
}
