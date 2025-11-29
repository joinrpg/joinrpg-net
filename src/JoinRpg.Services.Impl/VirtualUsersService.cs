using System.Data;
using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl;

public class VirtualUsersService(Func<IUnitOfWork> uowResolver) : IVirtualUsersService
{
    private readonly Lazy<User> _paymentsUser = new Lazy<User>(() => LoadUserByName(uowResolver(), User.OnlinePaymentVirtualUser), true);

    public User PaymentsUser => _paymentsUser.Value;

    private readonly Lazy<User> _robotUser = new Lazy<User>(() => LoadUserByName(uowResolver(), User.RobotVirtualUser), true);

    public User RobotUser => _robotUser.Value;

    UserIdentification IVirtualUsersService.RobotUserId => new UserIdentification(RobotUser.UserId);

    private static User LoadUserByName(IUnitOfWork uow, string userName)
    {
        return uow
                .GetDbSet<User>()
                .AsNoTracking()
                .Include(u => u.Auth)
                .Include(u => u.Allrpg)
                .Include(u => u.Extra)
                .SingleOrDefault(u => u.Email == userName)
            ?? throw new DataException($"Virtual user {userName} not found");
    }
}
