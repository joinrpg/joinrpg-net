using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.Interfaces.Email;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Email;
internal class AccountServiceEmailImpl(IOptions<NotificationsOptions> options, IEmailSendingService messageService) : IAccountEmailService
{
    private readonly RecepientData joinRpgSender = options.Value.ServiceRecepient;
    private readonly string joinRpgTeam = options.Value.JoinRpgTeamName;

    #region Account emails
    public Task Email(RemindPasswordEmail email)
    {
        var text = $@"Добрый день, {messageService.GetRecepientPlaceholderName()}, 

вы (или кто-то, выдающий себя за вас) запросил восстановление пароля на сайте JoinRpg.Ru. 
Если это вы, кликните <a href=""{email.CallbackUrl}"">вот по этой ссылке</a>, и мы восстановим вам пароль. 
Если вдруг вам пришло такое письмо, а вы не просили восстанавливать пароль, ничего страшного! Просто проигнорируйте его.

--

{joinRpgTeam}";
        User recipient = email.Recipient;
        return messageService.SendEmail("Восстановление пароля на JoinRpg.Ru",
            new MarkdownString(text),
            joinRpgSender,
            recipient.ToRecepientData());
    }

    public Task Email(ConfirmEmail email)
    {
        var text = $@"Здравствуйте, и добро пожаловать на joinrpg.ru!

Пожалуйста, подтвердите свой аккаунт, кликнув <a href=""{email.CallbackUrl}"">вот по этой ссылке</a>.

Это необходимо для того, чтобы мастера игр, на которые вы заявитесь, могли надежно связываться с вами.

Если вдруг вам пришло такое письмо, а вы нигде не регистрировались, ничего страшного! Просто проигнорируйте его.

--

{joinRpgTeam}";
        return messageService.SendEmail("Регистрация на JoinRpg.Ru",
            new MarkdownString(text),
            joinRpgSender,
            email.Recipient.ToRecepientData());
    }
    #endregion
}
