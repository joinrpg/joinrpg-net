using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot;

namespace JoinRpg.Common.Telegram.Test;

public class TelegramNotificationServiceImplTimeoutTest
{
    [Fact]
    public async Task SendTelegramNotification_WithTimeoutException_ReturnsCommonFailure()
    {
        var handler = new ThrowingHttpMessageHandler(new TimeoutException("Request timed out"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org") };
        var botClient = new TelegramBotClient("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", httpClient);
        var service = new TelegramNotificationServiceImpl(botClient, NullLogger<TelegramNotificationServiceImpl>.Instance);

        var result = await service.SendTelegramNotification(new TelegramId(1, null), new TelegramHtmlString("test"));

        result.ShouldBe(SendingResult.CommonFailure());
    }

    [Fact]
    public async Task SendTelegramNotification_WithInnerTimeoutException_ReturnsCommonFailure()
    {
        var inner = new TimeoutException("Request timed out");
        var outer = new HttpRequestException("Connection failed", inner);
        var handler = new ThrowingHttpMessageHandler(outer);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org") };
        var botClient = new TelegramBotClient("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", httpClient);
        var service = new TelegramNotificationServiceImpl(botClient, NullLogger<TelegramNotificationServiceImpl>.Instance);

        var result = await service.SendTelegramNotification(new TelegramId(1, null), new TelegramHtmlString("test"));

        result.ShouldBe(SendingResult.CommonFailure());
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromException<HttpResponseMessage>(exception);
    }
}
