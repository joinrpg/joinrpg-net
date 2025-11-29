using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.Interfaces.Email;

namespace JoinRpg.Services.Notifications;

public partial class NotificationServiceImpl(
    IUserRepository userRepository,
    IEmailSendingService emailSendingService,
    INotificationRepository notificationRepository) : INotificationService
{
    private record class NotificationRow(
        NotificationRecepient Recipient, UserDisplayName DisplayName, IReadOnlyCollection<NotificationAddress> Channels, Email Email);

    async Task INotificationService.QueueDirectNotification(NotificationEvent notificationMessage, NotificationChannel directChannel)
    {
        var templater = new NotifcationFieldsTemplater(notificationMessage.TemplateText);
        VerifyFieldsPresent(notificationMessage, templater);

        var users = await GetNotificationsForUsers(notificationMessage.Recepients, user => [directChannel]);

        await SendEmailsUsingLegacy(notificationMessage, users);

        await SaveToQueue(notificationMessage, templater, users, DateTimeOffset.UtcNow);
    }

    async Task INotificationService.QueueNotification(NotificationEvent notificationMessage)
    {
        var templater = new NotifcationFieldsTemplater(notificationMessage.TemplateText);
        VerifyFieldsPresent(notificationMessage, templater);

        var users = await GetNotificationsForUsers(notificationMessage.Recepients, user => [NotificationChannel.ShowInUi]); // все выключено, кроме UI

        await SendEmailsUsingLegacy(notificationMessage, users);

        await SaveToQueue(notificationMessage, templater, users, DateTimeOffset.UtcNow);
    }

    private async Task SaveToQueue(NotificationEvent notificationMessage, NotifcationFieldsTemplater templater, NotificationRow[] users, DateTimeOffset createdAt)
        => await notificationRepository.InsertNotifications(
                [.. users.Select(user => CreateMessageDto(notificationMessage, user, templater.Substitute(user.Recipient.Fields), createdAt))]);

    private async Task SendEmailsUsingLegacy(NotificationEvent notificationMessage, NotificationRow[] users)
    {
        var sender = await userRepository.GetRequiredUserInfo(notificationMessage.Initiator);
        await emailSendingService.SendEmails(
            notificationMessage.Header,
            new MarkdownString(notificationMessage.TemplateText.TemplateContents + $"\n--\n\n{sender.DisplayName.DisplayName}"),
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
            if (recepient.Fields.Keys.Except(fields).Any())
            {
                throw new InvalidOperationException("Too many fields");
            }
            if (fields.Except(recepient.Fields.Keys).Any())
            {
                throw new InvalidOperationException("Not enough fields");
            }
        }
    }

    private static NotificationMessageCreateDto CreateMessageDto(NotificationEvent notificationMessage, NotificationRow user, MarkdownString body, DateTimeOffset createdAt)
    {
        return new NotificationMessageCreateDto(

            new NotificationMessageForRecipient(
                body,
                notificationMessage.Initiator,
                notificationMessage.Header,
                user.Recipient.UserId,
                notificationMessage.EntityReference,
                createdAt),
            user.Channels

            );
    }

    private async Task<NotificationRow[]> GetNotificationsForUsers(
        NotificationRecepient[] recepients,
        Func<UserInfo, IReadOnlyCollection<NotificationChannel>> enabledChannelsSelector)
    {
        var recDict = recepients.ToDictionary(r => r.UserId, r => r);
        var r = await userRepository.GetRequiredUserInfos([.. recepients.Select(r => r.UserId)]);

        return [.. r.Select(user => ToNotificationRow(user, recDict))];

        NotificationRow ToNotificationRow(UserInfo user, Dictionary<UserIdentification, NotificationRecepient> recDict)
        {
            var enabledChannels = enabledChannelsSelector(user);
            return new NotificationRow(recDict[user.UserId], user.DisplayName, [.. GetChannels(user).Where(c => enabledChannels.Contains(c.Channel))], user.Email);
        }
    }

    private static IEnumerable<NotificationAddress> GetChannels(UserInfo user)
    {
        if (user.Social.TelegramId is not null)
        {
            //    yield return new NotificationAddress(user.Social.TelegramId);
        }

        if (user.Email is not null)
        {
            yield return new NotificationAddress(user.Email);
        }
    }
}
