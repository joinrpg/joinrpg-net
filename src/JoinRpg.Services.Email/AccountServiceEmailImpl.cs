using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Email;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Email;
internal class AccountServiceEmailImpl(IMailGunConfig config, IEmailSendingService messageService) : IAccountEmailService
{
    private readonly RecepientData _joinRpgSender = new(JoinRpgTeam, config.ServiceEmail);
    private const string JoinRpgTeam = "Команда JoinRpg.Ru";

    #region Account emails
    public Task Email(RemindPasswordEmail email)
    {
        var text = $@"Добрый день, {messageService.GetRecepientPlaceholderName()}, 

вы (или кто-то, выдающий себя за вас) запросил восстановление пароля на сайте JoinRpg.Ru. 
Если это вы, кликните <a href=""{email.CallbackUrl}"">вот по этой ссылке</a>, и мы восстановим вам пароль. 
Если вдруг вам пришло такое письмо, а вы не просили восстанавливать пароль, ничего страшного! Просто проигнорируйте его.

--

{JoinRpgTeam}";
        User recipient = email.Recipient;
        return messageService.SendEmail("Восстановление пароля на JoinRpg.Ru",
            new MarkdownString(text),
            _joinRpgSender,
            recipient.ToRecepientData());
    }

    public Task Email(ConfirmEmail email)
    {
        var text = $@"Здравствуйте, и добро пожаловать на joinrpg.ru!

Пожалуйста, подтвердите свой аккаунт, кликнув <a href=""{email.CallbackUrl}"">вот по этой ссылке</a>.

Это необходимо для того, чтобы мастера игр, на которые вы заявитесь, могли надежно связываться с вами.

Если вдруг вам пришло такое письмо, а вы нигде не регистрировались, ничего страшного! Просто проигнорируйте его.

--

{JoinRpgTeam}";
        return messageService.SendEmail("Регистрация на JoinRpg.Ru",
            new MarkdownString(text),
            _joinRpgSender,
            email.Recipient.ToRecepientData());
    }
    #endregion
}
