using System;
using System.Data;
using System.Linq;
using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
    /// <inheritdoc />
    public class VirtualUsersService : IVirtualUsersService
    {
        private readonly Lazy<User> _paymentsUser;

        /// <inheritdoc />
        public User PaymentsUser => _paymentsUser.Value;

        /// <summary>
        /// Default constructor
        /// </summary>
        public VirtualUsersService(Func<IUnitOfWork> uowResolver)
        {
            _paymentsUser = new Lazy<User>(() => LoadPaymentsUser(uowResolver()), true);
        }

        private User LoadPaymentsUser(IUnitOfWork uow)
        {
            User result = uow
                .GetDbSet<User>()
                .Include(u => u.Auth)
                .Include(u => u.Allrpg)
                .Include(u => u.Extra)
                .SingleOrDefault(u => u.Email == User.OnlinePaymentVirtualUser);
            if (result == null)
                throw new DataException("Virtual payments manager user was not found");
            return result;
        }
    }
}
