using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure.Files;

/// <summary>
/// Регрессия на #4995: именованный клиент для скачивания файлов не был нигде настроен,
/// поэтому IHttpClientFactory молча отдавал клиента с дефолтным таймаутом 100 секунд,
/// и зависший t.me держал запрос привязки телеграма 100 секунд.
/// </summary>
public class FileDownloaderTests
{
    [Fact]
    public void NamedClientIsConfiguredWithShortTimeout()
    {
        var factory = BuildServices().GetRequiredService<IHttpClientFactory>();

        var client = factory.CreateClient(FileDownloader.HttpClientName);

        client.Timeout.ShouldBe(FileDownloader.DownloadTimeout);
        client.Timeout.ShouldBeLessThan(TimeSpan.FromSeconds(100), "иначе это дефолтный таймаут — регистрация клиента снова потерялась");
    }

    [Fact]
    public async Task DownloaderAsksFactoryForTheNamedClient()
    {
        var factory = new RecordingHttpClientFactory();
        var downloader = new FileDownloader(factory, NullLogger<FileDownloader>.Instance);

        using var target = new MemoryStream();
        var contentType = await downloader.DownloadAsync(new Uri("https://t.me/i/userpic/320/test.jpg"), target, maxSize: 1024, CancellationToken.None);

        factory.RequestedName.ShouldBe(FileDownloader.HttpClientName);
        contentType.ShouldBe("image/jpeg");
        target.ToArray().ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public async Task OversizedFileThrowsFileDownloadException()
    {
        var downloader = new FileDownloader(new RecordingHttpClientFactory(), NullLogger<FileDownloader>.Instance);

        using var target = new MemoryStream();
        // Стаб отдаёт 3 байта
        _ = await Should.ThrowAsync<FileDownloadException>(
            () => downloader.DownloadAsync(new Uri("https://example.invalid/big.bin"), target, maxSize: 2, CancellationToken.None));
    }

    [Fact]
    public async Task MissingMediaTypeThrowsFileDownloadException()
    {
        var downloader = new FileDownloader(new NoMediaTypeHttpClientFactory(), NullLogger<FileDownloader>.Instance);

        using var target = new MemoryStream();
        _ = await Should.ThrowAsync<FileDownloadException>(
            () => downloader.DownloadAsync(new Uri("https://example.invalid/no-type"), target, maxSize: 1024, CancellationToken.None));
    }

    private static ServiceProvider BuildServices()
        => new ServiceCollection()
            .AddJoinS3Storage()
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

    private sealed class NoMediaTypeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubHandler(withMediaType: false));
    }

    private sealed class StubHandler(bool withMediaType = true) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = new ByteArrayContent([1, 2, 3]);
            if (withMediaType)
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }
}
