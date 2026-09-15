using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.Test.Fakes;

internal sealed class FakeVirtualUsersService : IVirtualUsersService
{
    public User PaymentsUser { get; } = new User { UserId = 1000, PrefferedName = "Payments", Email = "payments@example.com", Claims = [] };
    public User RobotUser => throw new NotSupportedException();
    public UserIdentification RobotUserId => new(int.MaxValue);
}
