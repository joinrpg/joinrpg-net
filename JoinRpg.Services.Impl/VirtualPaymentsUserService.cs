using System;
using System.Data;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
    /// <inheritdoc />
    public class VirtualPaymentsUserService : IVirtualPaymentsUserService
    {
        private readonly Lazy<Task<User>> _user;

        /// <inheritdoc />
        public Task<User> UserAsync => _user.Value;

        /// <summary>
        /// Default constructor
        /// </summary>
        public VirtualPaymentsUserService(Func<IUnitOfWork> uowResolver)
        {
            _user = new Lazy<Task<User>>(() => LoadVirtualUserAsync(uowResolver()), true);
        }

        private async Task<User> LoadVirtualUserAsync(IUnitOfWork uow)
        {
            User result = await uow
                .GetUsersRepository()
                .GetByEmail(User.VirtualOnlinePaymentsManagerEmail);
            if (result == null)
                throw new DataException("Virtual payments manager user was not found");
            return result;
        }
    }
}
