using Joinrpg.Web.Identity;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Portal.Test.Identity;

/// <summary>Запоминает, кому ушло письмо о смене пароля.</summary>
internal sealed class CaptureAccountEmailService : IAccountEmailService<JoinIdentityUser>
{
    public List<JoinIdentityUser> PasswordChanged { get; } = [];

    public Task PasswordChangedEmail(JoinIdentityUser user)
    {
        PasswordChanged.Add(user);
        return Task.CompletedTask;
    }

    public Task ResetPasswordEmail(JoinIdentityUser user, string callbackUrl) => throw new NotSupportedException();

    public Task ConfirmEmail(JoinIdentityUser user, string callbackUrl) => throw new NotSupportedException();
}
