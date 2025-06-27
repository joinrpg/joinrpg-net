using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.Interfaces.Email;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Options;

namespace Joinrpg.Web.Identity;
internal class AccountServiceEmailImpl(IOptions<NotificationsOptions> options, IEmailSendingService messageService) : IAccountEmailService<JoinIdentityUser>
{
    private readonly RecepientData joinRpgSender = options.Value.ServiceRecepient;
    private readonly string joinRpgTeam = options.Value.JoinRpgTeamName;

    #region Account emails
    public Task ResetPasswordEmail(JoinIdentityUser user, string callbackUrl)
    {
        var text = $@"Добрый день, {messageService.GetRecepientPlaceholderName()}, 

вы (или кто-то, выдающий себя за вас) запросил восстановление пароля на сайте JoinRpg.Ru. 
Если это вы, кликните <a href=""{callbackUrl}"">вот по этой ссылке</a>, и мы восстановим вам пароль. 
Если вдруг вам пришло такое письмо, а вы не просили восстанавливать пароль, ничего страшного! Просто проигнорируйте его.

--

{joinRpgTeam}";
        return messageService.SendEmail("Восстановление пароля на JoinRpg.Ru",
            new MarkdownString(text),
            joinRpgSender,
            ToRecipientData(user));
    }

    private static RecepientData ToRecipientData(JoinIdentityUser user) => new(user.DisplayName?.DisplayName ?? user.UserName, user.UserName);

    public Task ConfirmEmail(JoinIdentityUser user, string callbackUrl)
    {
        var text = $@"Здравствуйте, и добро пожаловать на joinrpg.ru!

Пожалуйста, подтвердите свой аккаунт, кликнув <a href=""{callbackUrl}"">вот по этой ссылке</a>.

Это необходимо для того, чтобы мастера игр, на которые вы заявитесь, могли надежно связываться с вами.

Если вдруг вам пришло такое письмо, а вы нигде не регистрировались, ничего страшного! Просто проигнорируйте его.

--

{joinRpgTeam}";
        return messageService.SendEmail("Регистрация на JoinRpg.Ru",
            new MarkdownString(text),
            joinRpgSender,
            ToRecipientData(user));
    }
    #endregion
}
