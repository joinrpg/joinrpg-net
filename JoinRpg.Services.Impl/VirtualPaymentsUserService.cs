using System.Data;
using System.Web.Mvc;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
    /// <inheritdoc />
    public class VirtualPaymentsUserService : IVirtualPaymentsUserService
    {
        /// <inheritdoc />
        public User User { get; protected set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public VirtualPaymentsUserService()
        {
            using (IUnitOfWork uow = DependencyResolver.Current.GetService<IUnitOfWork>())
            {
                User = uow?.GetUsersRepository()
                    .GetByEmail(User.VirtualOnlinePaymentsManagerEmail)
                    .GetAwaiter()
                    .GetResult();

                if (User == null)
                    throw new DataException("Virtual payments manager user was not found");
            }
        }
    }
}
