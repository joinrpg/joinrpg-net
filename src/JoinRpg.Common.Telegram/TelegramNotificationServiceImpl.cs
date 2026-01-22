using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace JoinRpg.Common.Telegram;

internal class TelegramNotificationServiceImpl(TelegramBotClient client, ILogger<TelegramNotificationServiceImpl> logger) : ITelegramNotificationService
{
    public async Task SendTelegramNotification(TelegramId telegramId, TelegramHtmlString contents)
    {
        try
        {
            _ = await client.SendMessage(new ChatId(telegramId.Id), contents.SanitizeHtml(), ParseMode.Html, linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true });
            logger.LogInformation("Отправлено сообщение пользователю в телеграм {telegramId}", telegramId);
        }
        catch (ApiRequestException exception)
        {
            logger.LogError(exception, "Ошибка при отправке сообщения в телеграм {telegramId}. Error code={telegramErrorCode}, Параметры={telegramResponseParameters}",
                telegramId,
                exception.ErrorCode,
                exception.Parameters
                );
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ошибка при отправке сообщения в телеграм {telegramId} ", telegramId);
        }
    }
}

internal class StubTelegramNotificationService(ILogger<StubTelegramNotificationService> logger) : ITelegramNotificationService
{
    public Task SendTelegramNotification(TelegramId telegramId, TelegramHtmlString contents)
    {
        logger.LogInformation("Отправлено сообщение пользователю в телеграм {telegramId}: {message}", telegramId, contents);
        return Task.CompletedTask;
    }
}
