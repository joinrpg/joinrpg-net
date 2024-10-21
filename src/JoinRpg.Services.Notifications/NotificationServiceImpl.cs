using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Interfaces.Email;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Notifications;

namespace JoinRpg.Services.Notifications;

public partial class NotificationServiceImpl(
    IUserRepository userRepository,
    IEmailSendingService emailSendingService,
    INotificationRepository notificationRepository) : INotifcationService
{
    private record class NotificationRow(
        UserIdentification UserIdentification, NotificationRecepient Recepient, Email? Email, TelegramId? TelegramId, UserDisplayName DisplayName);

    async Task INotifcationService.QueueNotification(NotificationMessage notificationMessage)
    {
        var templater = new NotifcationFieldsTemplater(notificationMessage.Text);

        var users = await GetNotificationsForUsers(notificationMessage.Recepients);
        var sender = (await userRepository.GetUsersNotificationInfo([notificationMessage.Initiator])).Single();

        VerifyFieldsPresent();

        await SendEmailsUsingLegacy();

        await SaveToQueue();

        async Task SendEmailsUsingLegacy()
        {
            await emailSendingService.SendEmails(
                notificationMessage.Header,
                notificationMessage.Text,
                new RecepientData(sender.DisplayName, sender.Email),
                users
                .Where(u => u.Email is not null)
                .Select(u => new RecepientData(u.DisplayName, u.Email!, u.Recepient.Fields)).ToArray());
        }

        async Task SaveToQueue() => await notificationRepository.InsertNotifications(
            [..
            users.Select(user => CreateMessageDto(notificationMessage, user, sender, templater.Substitute(user.Recepient.Fields, user.DisplayName)))]);

        void VerifyFieldsPresent()
        {
            string[] fields = templater.GetFields();

            foreach (var recepient in notificationMessage.Recepients)
            {
                if (recepient.Fields.Values.Except(fields).Any())
                {
                    throw new InvalidOperationException("Not enough fields");
                }
                if (fields.Except(recepient.Fields.Values).Any())
                {
                    throw new InvalidOperationException("Too many fields");
                }
            }
        }
    }

    private NotificationMessageDto CreateMessageDto(NotificationMessage notificationMessage, NotificationRow user, UserNotificationInfoDto sender, MarkdownString body)
    {
        return new NotificationMessageDto()
        {
            Body = body,
            Header = notificationMessage.Header,
            Initiator = notificationMessage.Initiator,
            InitiatorAddress = sender.Email,
            Recepient = user.UserIdentification,
            Channels = GetChannels(user).ToArray(),
        };

        static IEnumerable<NotificationChannelDto> GetChannels(NotificationRow user)
        {
            //TODO Add Email channel here
            if (user.TelegramId is not null)
            {
                yield return new(NotificationChannel.Telegram, user.TelegramId.Id.ToString());
            }
        }
    }

    private async Task<NotificationRow[]> GetNotificationsForUsers(NotificationRecepient[] recepients)
    {
        var recDict = recepients.ToDictionary(r => r.UserId, r => r);
        var r = await userRepository.GetUsersNotificationInfo(recepients.Select(r => r.UserId).ToArray());

        return r.Select(user => new NotificationRow(user.UserId, recDict[user.UserId], user.Email, user.TelegramId, user.DisplayName)).ToArray();
    }
}
