namespace JoinRpg.Services.Interfaces.Notification;

public interface IAccountEmailService
{
    Task Email(RemindPasswordEmail email);
    Task Email(ConfirmEmail email);
}
