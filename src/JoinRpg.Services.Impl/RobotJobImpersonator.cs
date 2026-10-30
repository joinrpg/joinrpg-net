using JoinRpg.Domain;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Имперсонация джоб: джобы работают под роботом-администратором (см. ADR002).
/// Живёт в scope вместе с <see cref="IImpersonateAccessor"/> — раннер зовёт
/// <see cref="StartAsRobot"/> до запуска джобы и <see cref="Stop"/> после.
/// </summary>
public class RobotJobImpersonator(IVirtualUsersService virtualUsersService, IImpersonateAccessor impersonateAccessor) : IJobImpersonator
{
    public void StartAsRobot()
    {
        DataModel.User robotUser = virtualUsersService.RobotUser;
        impersonateAccessor.StartImpersonate(robotUser.GetId(), robotUser.ExtractDisplayName(), AvatarIdentification.FromOptional(robotUser.SelectedAvatarId), robotUser.Auth.IsAdmin);
    }

    public void Stop() => impersonateAccessor.StopImpersonate();
}
