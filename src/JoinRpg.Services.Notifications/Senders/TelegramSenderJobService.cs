using JoinRpg.Common.Telegram;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Notifications.Senders;
internal class TelegramSenderJobService(
    IOptions<TelegramLoginOptions> telegramLoginOptions,
    ITelegramNotificationService telegramNotificationService
    ) : ISenderJob
{
    public static NotificationChannel Channel => NotificationChannel.Telegram;

    public bool Enabled => telegramLoginOptions.Value.Enabled;

    public async Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken)
    {
        var htmlString = message.Message.Body.ToHtmlString();
        //TODO Здесь подпись нужна и тест на нее
        await telegramNotificationService.SendTelegramNotification(message.NotificationAddress.AsTelegram(), new TelegramHtmlString(htmlString.Value));
        return SendingResult.Success();
    }
}
