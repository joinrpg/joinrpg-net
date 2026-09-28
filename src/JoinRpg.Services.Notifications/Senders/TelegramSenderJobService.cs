using System.Net;
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
        // Заголовок — жирным и отдельной строкой, потом пустая строка, тело, ссылка, затем (если не
        // пропущена) подпись курсивом. Теги <strong>/<em>/<a> переживают санитайзер Telegram
        // (см. HtmlSanitizers.InitTelegramSanitizer).
        // Заголовок — обычный текст, а не markdown, поэтому размечаем его сразу как HTML: иначе
        // символы вроде * или _ внутри темы рассылки развалили бы выделение.
        var linkPart = entityLink is null ? "" : $"\n\n{entityLink.Markdown.Value}";
        var signaturePart = displayName is null ? "" : $"\n\n_{displayName.DisplayName}_";
        var bodyHtml = new MarkdownString($"{body.Value}{linkPart}{signaturePart}").ToTelegramHtmlString();
        return new TelegramHtmlString($"<strong>{WebUtility.HtmlEncode(header)}</strong>\n\n{bodyHtml.Contents}");
    }
}
