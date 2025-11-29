using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DataModel;
using JoinRpg.Interfaces.Email;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Services.Notifications;

public partial class NotificationServiceImpl(
    IUserRepository userRepository,
    IEmailSendingService emailSendingService,
    INotificationRepository notificationRepository) : INotificationService
{
    private record class NotificationRow(
        NotificationRecepient Recipient, UserDisplayName DisplayName, IReadOnlyCollection<NotificationAddress> Channels, Email Email);

    async Task INotificationService.QueueNotification(NotificationEvent notificationMessage)
    {
        var templater = new NotifcationFieldsTemplater(notificationMessage.TemplateText);
        VerifyFieldsPresent(notificationMessage, templater);

        var users = await GetNotificationsForUsers(notificationMessage.Recepients);

        await SendEmailsUsingLegacy(notificationMessage, users);

        await SaveToQueue(notificationMessage, templater, users);
    }

    private async Task SaveToQueue(NotificationEvent notificationMessage, NotifcationFieldsTemplater templater, NotificationRow[] users)
        => await notificationRepository.InsertNotifications(
                [.. users.Select(user => CreateMessageDto(notificationMessage, user, templater.Substitute(user.Recipient.Fields, user.DisplayName)))]);

    private async Task SendEmailsUsingLegacy(NotificationEvent notificationMessage, NotificationRow[] users)
    {
        var sender = await userRepository.GetRequiredUserInfo(notificationMessage.Initiator);
        await emailSendingService.SendEmails(
            notificationMessage.Header,
            notificationMessage.TemplateText,
            new RecepientData(sender.DisplayName, sender.Email),
            [.. users
            .Where(u => u.Email is not null)
            .Select(u => new RecepientData(u.DisplayName, u.Email!, u.Recipient.Fields))]);
    }

    private static void VerifyFieldsPresent(NotificationEvent notificationMessage, NotifcationFieldsTemplater templater)
    {
        var fields = templater.GetFields();

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

    private static NotificationMessageCreateDto CreateMessageDto(NotificationEvent notificationMessage, NotificationRow user, MarkdownString body)
    {
        return new NotificationMessageCreateDto(

            new NotificationMessageForRecipient(body, notificationMessage.Initiator, notificationMessage.Header, user.Recipient.UserId),
            user.Channels

            );
    }

    private async Task<NotificationRow[]> GetNotificationsForUsers(NotificationRecepient[] recepients)
    {
        var recDict = recepients.ToDictionary(r => r.UserId, r => r);
        var r = await userRepository.GetRequiredUserInfos([.. recepients.Select(r => r.UserId)]);

        return [.. r.Select(user => new NotificationRow(recDict[user.UserId], user.DisplayName, [.. GetChannels(user)], user.Email))];

        static IEnumerable<NotificationAddress> GetChannels(UserInfo user)
        {

            //TODO Все включить
            if (user.Social.TelegramId is not null && false)
            {
                //yield return new NotificationAddress(user.Social.TelegramId);
            }

            if (user.Email is not null && false)
            {
                yield return new NotificationAddress(user.Email);
            }
        }
    }
}
