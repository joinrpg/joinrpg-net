using System.Text.RegularExpressions;
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
        MatchCollection matchCollection = FieldPlaceholderRegex().Matches(notificationMessage.Text.Contents!);

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
                .Select(u => new RecepientData(u.DisplayName, u.Email!, u.Recepient.UserFields)).ToArray());
        }

        async Task SaveToQueue() => await notificationRepository.InsertNotifications([.. users.Select(user => CreateMessageDto(notificationMessage, user, sender))]);

        void VerifyFieldsPresent()
        {
            string[] fields = [.. matchCollection.Select(m => m.Value)];

            foreach (var recepient in notificationMessage.Recepients)
            {
                if (recepient.UserFields.Values.Except(fields).Any())
                {
                    throw new InvalidOperationException("Not enough fields");
                }
                if (fields.Except(recepient.UserFields.Values).Any())
                {
                    throw new InvalidOperationException("Too many fields");
                }
            }
        }
    }

    private NotificationMessageDto CreateMessageDto(NotificationMessage notificationMessage, NotificationRow user, UserNotificationInfoDto sender)
    {
        return new NotificationMessageDto()
        {
            Body = SubsititeUserValues(notificationMessage.Text, user.Recepient.UserFields, user.DisplayName),
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

    private MarkdownString SubsititeUserValues(MarkdownString text, IReadOnlyDictionary<string, string> userFields, UserDisplayName displayName) => throw new NotImplementedException();

    private async Task<NotificationRow[]> GetNotificationsForUsers(NotificationRecepient[] recepients)
    {
        var recDict = recepients.ToDictionary(r => r.UserId, r => r);
        var r = await userRepository.GetUsersNotificationInfo(recepients.Select(r => r.UserId).ToArray());

        return r.Select(user => new NotificationRow(user.UserId, recDict[user.UserId], user.Email, user.TelegramId, user.DisplayName)).ToArray();
    }



    [GeneratedRegex("%recepient.(\\w+?)%", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FieldPlaceholderRegex();
}
