using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces.Notification;

public class RemindPasswordEmail
{
    public string CallbackUrl { get; set; }
    public User Recipient { get; set; }
}

public class ConfirmEmail
{
    public string CallbackUrl
    { get; set; }
    public User Recipient
    { get; set; }
}
