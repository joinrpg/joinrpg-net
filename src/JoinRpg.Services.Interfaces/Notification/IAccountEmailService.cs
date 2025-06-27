namespace JoinRpg.Services.Interfaces.Notification;

public interface IAccountEmailService<TUser>
{
    Task ResetPasswordEmail(TUser user, string callbackUrl);
    Task ConfirmEmail(TUser user, string callbackUrl);
}
