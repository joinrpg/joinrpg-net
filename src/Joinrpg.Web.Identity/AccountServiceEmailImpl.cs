using JoinRpg.Interfaces;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Options;

namespace Joinrpg.Web.Identity;
internal class AccountServiceEmailImpl(IOptions<NotificationsOptions> options, INotificationService messageService, IVirtualUsersService virtualUsersService) : IAccountEmailService<JoinIdentityUser>
{
    public Task ResetPasswordEmail(JoinIdentityUser user, string callbackUrl)
    {
        // Эти и другие email должны читаться в plain text режиме. Для этого нужно, чтобы ссылки были отдельно от HTML и не были спрятаны
        // Хорошо бы научить вырезалку markdown отображать URL корректно https://github.com/xoofx/markdig/issues/882
        var text = $@"Добрый день, {user.UserName}, 

вы (или кто-то, выдающий себя за вас) запросил восстановление пароля на сайте JoinRpg.Ru. 
Если это вы, кликните по ссылке ниже, чтобы восстановить пароль:

{callbackUrl}

Если вдруг вам пришло такое письмо, а вы не просили восстанавливать пароль, ничего страшного! Просто проигнорируйте его.
";

        return SendAccountNotification(user, text, "Восстановление пароля на JoinRpg.Ru");
    }

    public Task ConfirmEmail(JoinIdentityUser user, string callbackUrl)
    {
        var text = $@"Здравствуйте, и добро пожаловать на joinrpg.ru!

Пожалуйста, подтвердите свой аккаунт, кликнув по сссылке:

{callbackUrl}

Это необходимо для того, чтобы мастера игр, на которые вы заявитесь, могли надежно связываться с вами.

Если вдруг вам пришло такое письмо, а вы нигде не регистрировались, ничего страшного! Просто проигнорируйте его.
";
        return SendAccountNotification(user, text, "Регистрация на JoinRpg.Ru");
    }

    private Task SendAccountNotification(JoinIdentityUser user, string text, string header)
    {
        text += $"\n--\n\n{options.Value.JoinRpgTeamName}";

        var notification = new NotificationEvent(NotificationClass.UserAccount,
                                                 EntityReference: null,
                                                 header,
                                                 new NotificationEventTemplate(text),
                                                 [NotificationRecepient.Direct(new(user.Id))],
                                                 virtualUsersService.RobotUserId);

        return messageService.QueueDirectNotification(notification, NotificationChannel.Email);
    }
}
