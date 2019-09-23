using System;
using System.Data;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
    /// <inheritdoc />
    public class VirtualPaymentsUserService : IVirtualPaymentsUserService
    {
        /// <inheritdoc />
        public Lazy<User> User { get; protected set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public VirtualPaymentsUserService(Func<IUnitOfWork> uowResolver)
        {
            User = new Lazy<User>(() => LoadVirtualUser(uowResolver()), true);
        }

        private User LoadVirtualUser(IUnitOfWork uow)
        {
            User result = uow
                .GetUsersRepository()
                .GetByEmail(DataModel.User.VirtualOnlinePaymentsManagerEmail)
                .GetAwaiter()
                .GetResult();
            if (result == null)
                throw new DataException("Virtual payments manager user was not found");
            return result;
        }
    }
}
