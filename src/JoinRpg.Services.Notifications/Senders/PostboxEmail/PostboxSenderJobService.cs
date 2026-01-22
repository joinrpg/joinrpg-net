
using Amazon.SimpleEmailV2.Model;
using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Notifications.Senders.PostboxEmail;

internal class PostboxSenderJobService(
    IOptions<PostboxOptions> options,
    PostboxClientFactory postboxClientFactory,
    ILogger<PostboxSenderJobService> logger,
    IUserRepository userRepository,
    IOptions<NotificationsOptions> notificationOptions
    ) : ISenderJob
{
    static NotificationChannel ISenderJob.Channel => NotificationChannel.Email;

    bool ISenderJob.Enabled => options.Value.Enabled;

    async Task<SendingResult> ISenderJob.SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken)
    {
        var client = postboxClientFactory.Get();
        var sender = await userRepository.GetRequiredUserInfo(message.Message.Initiator);
        Body body = FormatBody(message.Message.Body, sender.DisplayName);

        var request = new SendEmailRequest
        {
            Destination = new Destination
            {
                ToAddresses = [message.NotificationAddress.AsEmail().Value]
            },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Body = body,
                    Subject = ToContent(message.Message.Header),
                }
            },
            FromEmailAddress = FormatAddress(sender.DisplayName.DisplayName, notificationOptions.Value.ServiceAccountEmail),
            ReplyToAddresses = [FormatAddress(sender.DisplayName.DisplayName, sender.Email.Value)],
        };

        var response = await client.SendEmailAsync(request, stoppingToken);

        logger.LogInformation("Отправка сообщения {notificationMessage} на адрес {recipientEmail} успешна {sesMessageId}", message.MessageId, message.NotificationAddress.AsEmail(), response.MessageId);

        return SendingResult.Success();
    }

    internal static Body FormatBody(MarkdownString bodyString, UserDisplayName displayName)
    {
        var signature = $"\n\n---\n\n{displayName.DisplayName}";
        var emailBody = new MarkdownString(bodyString.Contents + signature);

        var body = new Body
        {
            Text = ToContent(bodyString.ToPlainTextWithoutHtmlEscape() + signature), // Экранировать HTML в plain text email не нужно
            Html = ToContent(emailBody.ToHtmlString().Value),
        };
        return body;
    }

    private static Content ToContent(string text)
    {
        return new Content
        {
            Charset = "UTF-8",
            Data = text,
        };
    }

    private static string FormatAddress(string addressName, string email) => $"{addressName} <{email}>";
}
