using JoinRpg.Common.Telegram;
using JoinRpg.Data.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Notifications.Senders;

internal class TelegramSenderJobService(
    IOptions<TelegramLoginOptions> telegramLoginOptions,
    ITelegramNotificationService telegramNotificationService,
    IUserRepository userRepository,
    INotificationEntityLinkRenderer linkRenderer
    ) : ISenderJob
{
    public static NotificationChannel Channel => NotificationChannel.Telegram;

    public NotificationChannel InstanceChannel => Channel;

    public bool Enabled => telegramLoginOptions.Value.Enabled;

    public async Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken)
    {
        var entityLink = linkRenderer.RenderEntityLink(message.Message.EntityReference);
        TelegramHtmlString html;
        if (message.Message.SkipSignature)
        {
            html = FormatMessage(message.Message.Header, message.Message.Body, entityLink);
        }
        else
        {
            var sender = await userRepository.GetRequiredUserInfo(message.Message.Initiator);
            html = FormatMessage(message.Message.Header, message.Message.Body, entityLink, sender.DisplayName);
        }

        return await telegramNotificationService.SendTelegramNotification(message.NotificationAddress.AsTelegram(), html);
    }

    internal static TelegramHtmlString FormatMessage(string header, MarkdownString body, RenderedEntityLink? entityLink, UserDisplayName? displayName = null)
    {
        // Заголовок — жирным, тело, ссылка, затем (если не пропущена) подпись курсивом. Теги
        // <strong>/<em>/<a> переживают санитайзер Telegram (см. HtmlSanitizers.InitTelegramSanitizer).
        var linkPart = entityLink is null ? "" : $"\n\n{entityLink.Markdown.Value}";
        var signaturePart = displayName is null ? "" : $"\n\n_{displayName.DisplayName}_";
        var markdown = new MarkdownString($"**{header}**\n\n{body.Value}{linkPart}{signaturePart}");
        return new TelegramHtmlString(markdown.ToHtmlString().Value);
    }
}
