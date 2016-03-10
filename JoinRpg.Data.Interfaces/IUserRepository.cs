using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
   public interface IUserRepository
   {
     Task<User> GetById(int id);

     Task<User> WithProfile(int userId);
     Task<User> GetWithSubscribe(int currentUserId);
     [ItemCanBeNull]
     Task<User> GetByEmail(string email);
     Task<User> GetByAllRpgId(int allrpgId);
   }
}
