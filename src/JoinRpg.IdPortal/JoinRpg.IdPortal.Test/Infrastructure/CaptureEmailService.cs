using Joinrpg.Web.Identity;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.IdPortal.Test.Infrastructure;

public class CaptureEmailService : IAccountEmailService<JoinIdentityUser>
{
    public string? LastResetPasswordUrl { get; private set; }
    public string? LastConfirmEmailUrl { get; private set; }

    public Task ResetPasswordEmail(JoinIdentityUser user, string callbackUrl)
    {
        LastResetPasswordUrl = callbackUrl;
        return Task.CompletedTask;
    }

    public Task ConfirmEmail(JoinIdentityUser user, string callbackUrl)
    {
        LastConfirmEmailUrl = callbackUrl;
        return Task.CompletedTask;
    }
}
