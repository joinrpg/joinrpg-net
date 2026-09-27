using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.BlobStorage;

/// <summary>
/// Регрессия на #4995: именованный клиент download-avatar-client не был нигде настроен,
/// поэтому IHttpClientFactory молча отдавал клиента с дефолтным таймаутом 100 секунд,
/// и зависший t.me держал запрос привязки телеграма 100 секунд.
/// </summary>
public class AvatarDownloaderHttpClientTests
{
    [Fact]
    public void NamedAvatarClientIsConfiguredWithShortTimeout()
    {
        var factory = BuildServices().GetRequiredService<IHttpClientFactory>();

        var client = factory.CreateClient(AvatarDownloader.HttpClientName);

        client.Timeout.ShouldBe(AvatarDownloader.DownloadTimeout);
        client.Timeout.ShouldBeLessThan(TimeSpan.FromSeconds(100), "иначе это дефолтный таймаут — регистрация клиента снова потерялась");
    }

    [Fact]
    public async Task DownloaderAsksFactoryForTheNamedClient()
    {
        var factory = new RecordingHttpClientFactory();
        var downloader = new AvatarDownloader(factory, NullLogger<AvatarDownloader>.Instance);

        using var target = new MemoryStream();
        var (contentType, extension) = await downloader.DownloadAvatarAsync(new Uri("https://t.me/i/userpic/320/test.jpg"), target, CancellationToken.None);

        factory.RequestedName.ShouldBe(AvatarDownloader.HttpClientName);
        contentType.ShouldBe("image/jpeg");
        extension.ShouldBe(".jpeg");
    }

    private static ServiceProvider BuildServices()
        => new ServiceCollection()
            .AddJoinBlobStorage()
            .BuildServiceProvider();

    private sealed class RecordingHttpClientFactory : IHttpClientFactory
    {
        public string? RequestedName { get; private set; }

        public HttpClient CreateClient(string name)
        {
            RequestedName = name;
            return new HttpClient(new StubHandler());
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = new ByteArrayContent([1, 2, 3]);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }
}
