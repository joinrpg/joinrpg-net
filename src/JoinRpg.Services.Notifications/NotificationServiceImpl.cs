using System.Diagnostics;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.Interfaces;
using JoinRpg.Interfaces.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Notifications;

public partial class NotificationServiceImpl(
    IUserRepository userRepository,
    IEmailSendingService emailSendingService,
    INotificationRepository notificationRepository,
    IOptions<NotificationsOptions> notificationOptions,
    IOptions<PostboxOptions> postboxOptions,
    ILogger<NotificationServiceImpl> logger
    ) : INotificationService
{
    private readonly ActivitySource activitySource = new(nameof(NotificationServiceImpl));

    private record class NotificationRow(
        NotificationRecepient Recipient, UserDisplayName DisplayName, IReadOnlyCollection<NotificationAddress> Channels, Email Email);

    public async Task QueueDirectNotification(NotificationEvent notificationMessage, NotificationChannel directChannel)
    {
        using var activity = activitySource.StartActivity(nameof(QueueDirectNotification));
        logger.LogInformation("Собираемся отправить в {notificationChannel} сообщение {notificationMessage}", directChannel, notificationMessage);

        var templater = new NotifcationFieldsTemplater(notificationMessage.TemplateText);
        VerifyFieldsPresent(notificationMessage, templater);

        var users = await GetNotificationsForUsers(notificationMessage.Recepients, user => [directChannel]);

        await SendEmailsUsingLegacy(notificationMessage, users);

        await SaveToQueue(notificationMessage, templater, users, DateTimeOffset.UtcNow);
    }

    public async Task QueueNotification(NotificationEvent notificationMessage)
    {
        using var activity = activitySource.StartActivity(nameof(QueueNotification));
        logger.LogInformation("Собираемся отправить во все каналы сообщение {notificationMessage}", notificationMessage);


        var templater = new NotifcationFieldsTemplater(notificationMessage.TemplateText);
        VerifyFieldsPresent(notificationMessage, templater);

        var users = await GetNotificationsForUsers(notificationMessage.Recepients, user => [
            NotificationChannel.ShowInUi,
            NotificationChannel.Email,
            NotificationChannel.Telegram,
            ]);

        await SendEmailsUsingLegacy(notificationMessage, users);

        await SaveToQueue(notificationMessage, templater, users, DateTimeOffset.UtcNow);
    }

    private async Task SaveToQueue(NotificationEvent notificationMessage, NotifcationFieldsTemplater templater, NotificationRow[] users, DateTimeOffset createdAt)
        => await notificationRepository.InsertNotifications(
                [.. users.Select(user => CreateMessageDto(notificationMessage, user, templater.Substitute(user.Recipient.Fields), createdAt))]);

    private async Task SendEmailsUsingLegacy(NotificationEvent notificationMessage, NotificationRow[] users)
    {
        if (postboxOptions.Value.Enabled)
        {
            return; // Если postbox почему-то выключен, дублировать письма через старый механизм
        }
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
            // Ничего страшного, если нам передадут лишние поля, которых нет в темплейте, но в обратную сторону это проблема
            if (fields.Except(recepient.Fields.Keys).Any())
            {
                throw new InvalidOperationException($"Not enough fields: {string.Join(", ", fields.Except(recepient.Fields.Keys))}");
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
        IReadOnlyCollection<NotificationRecepient> recepients,
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

    private IEnumerable<NotificationAddress> GetChannels(UserInfo user)
    {
        yield return NotificationAddress.Ui();
        if (user.Social.TelegramId is not null)
        {
            yield return new NotificationAddress(user.Social.TelegramId);
        }

        if (user.Email is not null)
        {
            if (notificationOptions.Value.EmailWhiteList.Length == 0 || notificationOptions.Value.EmailWhiteList.Contains(user.Email))
            {
                yield return new NotificationAddress(user.Email);
            }

        }
    }
}
