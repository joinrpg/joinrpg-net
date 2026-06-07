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

    public bool Enabled => telegramLoginOptions.Value.Enabled;

    public async Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken)
    {
        var sender = await userRepository.GetRequiredUserInfo(message.Message.Initiator);
        var entityLink = linkRenderer.RenderEntityLink(message.Message.EntityReference);
        var html = FormatMessage(message.Message.Header, message.Message.Body, sender.DisplayName, entityLink);
        await telegramNotificationService.SendTelegramNotification(message.NotificationAddress.AsTelegram(), html);
        return SendingResult.Success();
    }

    internal static TelegramHtmlString FormatMessage(string header, MarkdownString body, UserDisplayName displayName, RenderedEntityLink? entityLink)
    {
        // Заголовок — жирным, тело, ссылка, затем подпись курсивом. Теги <strong>/<em>/<a>
        // переживают санитайзер Telegram (см. HtmlSanitizers.InitTelegramSanitizer).
        var linkPart = entityLink is null ? "" : $"\n\n{entityLink.Markdown.Contents}";
        var markdown = new MarkdownString($"**{header}**\n\n{body.Contents}{linkPart}\n\n_{displayName.DisplayName}_");
        return new TelegramHtmlString(markdown.ToHtmlString().Value);
    }
}
