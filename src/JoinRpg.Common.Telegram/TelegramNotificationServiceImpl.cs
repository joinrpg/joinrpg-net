using System.Diagnostics;
using System.Diagnostics.Metrics;
using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace JoinRpg.Common.Telegram;

internal class TelegramNotificationServiceImpl(TelegramBotClient client, ILogger<TelegramNotificationServiceImpl> logger) : ITelegramNotificationService
{
    private const int TelegramMaxMessageLength = 4096;

    private static readonly Meter meter = new("JoinRpg");
    private static readonly Histogram<double> sendDurationHistogram = meter.CreateHistogram<double>("telegram.send_duration_ms", "ms");
    private static readonly Counter<int> sendErrorsCounter = meter.CreateCounter<int>("telegram.send_errors");

    private static void CountError(string errorType) =>
        sendErrorsCounter.Add(1, new KeyValuePair<string, object?>("error_type", errorType));

    public async Task<string?> GetMyUserName(CancellationToken cancellationToken) => (await client.GetMe(cancellationToken)).Username;

    public async Task SendTelegramNotification(TelegramId telegramId, TelegramHtmlString contents)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            _ = await client.SendMessage(new ChatId(telegramId.Id), contents.SanitizeHtml(TelegramMaxMessageLength), ParseMode.Html, linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true });
            logger.LogInformation("Отправлено сообщение пользователю в телеграм {telegramId}", telegramId);
        }
        catch (ApiRequestException exception)
        {
            CountError("api");
            logger.LogWarning(exception, "Ошибка при отправке сообщения в телеграм {telegramId}. Error code={telegramErrorCode}, Параметры={telegramResponseParameters}",
                telegramId,
                exception.ErrorCode,
                exception.Parameters);
            throw;
        }
        catch (Exception exception) when (HasTimeoutInChain(exception))
        {
            CountError("timeout");
            logger.LogWarning(exception, "Таймаут при отправке сообщения в телеграм {telegramId}", telegramId);
            throw;
        }
        catch (Exception exception)
        {
            CountError("other");
            logger.LogWarning(exception, "Ошибка при отправке сообщения в телеграм {telegramId}", telegramId);
            throw;
        }
        finally
        {
            sw.Stop();
            sendDurationHistogram.Record(sw.Elapsed.TotalMilliseconds);
        }
    }

    private static bool HasTimeoutInChain(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is TimeoutException)
            {
                return true;
            }
        }
        return false;
    }
}

internal class StubTelegramNotificationService(ILogger<StubTelegramNotificationService> logger) : ITelegramNotificationService
{
    public Task<string?> GetMyUserName(CancellationToken cancellationToken) => Task.FromResult<string?>(null);

    public Task SendTelegramNotification(TelegramId telegramId, TelegramHtmlString contents)
    {
        logger.LogInformation("Отправлено сообщение пользователю в телеграм {telegramId}: {message}", telegramId, contents);
        return Task.CompletedTask;
    }
}
