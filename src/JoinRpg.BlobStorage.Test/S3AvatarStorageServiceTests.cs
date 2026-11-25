using JoinRpg.Common.WebInfrastructure.Files;
using JoinRpg.Common.WebInfrastructure.S3;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace JoinRpg.BlobStorage;

/// <summary>
/// Аватарная политика поверх общих IFileDownloader/IFileS3Uploader: лимит размера,
/// whitelist image-типов, имя файла. Ретраи и репортинг в S3 — забота FileS3Uploader.
/// </summary>
public class S3AvatarStorageServiceTests
{
    private static readonly Uri RemoteUri = new("https://example.invalid/avatar.jpg");

    [Fact]
    public async Task UnknownMediaTypeReturnsNull()
    {
        var uploader = new StubUploader();
        var service = CreateService(new StubDownloader("text/html"), uploader);

        var result = await service.StoreAvatar(RemoteUri);

        result.ShouldBeNull();
        uploader.UploadedKey.ShouldBeNull("не-image контент не должен попадать в хранилище");
    }

    [Fact]
    public async Task DownloadFailureReturnsNull()
    {
        var service = CreateService(new StubDownloader(throwOnDownload: true), new StubUploader());

        var result = await service.StoreAvatar(RemoteUri);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SuccessfulUploadReturnsUri()
    {
        var uploader = new StubUploader();
        var service = CreateService(new StubDownloader("image/jpeg"), uploader);

        var result = await service.StoreAvatar(RemoteUri);

        uploader.UploadedKey.ShouldNotBeNull().ShouldEndWith(".jpeg");
        uploader.UploadedContentType.ShouldBe("image/jpeg");
        result.ShouldNotBeNull();
        result.AbsoluteUri.ShouldBe($"https://s3.example.invalid/bucket/{uploader.UploadedKey}");
    }

    [Fact]
    public async Task UploadFailurePropagates()
    {
        // Сервис ловит только FileDownloadException; сбои загрузки (после ретраев uploader'а) идут наружу
        var service = CreateService(new StubDownloader("image/jpeg"), new StubUploader(throwOnUpload: true));

        _ = await Should.ThrowAsync<InvalidOperationException>(() => service.StoreAvatar(RemoteUri));
    }

    private static S3AvatarStorageService CreateService(StubDownloader downloader, StubUploader uploader)
        => new(downloader,
            Options.Create(new S3StorageOptions
            {
                Endpoint = "https://s3.example.invalid",
                AccessKey = "key",
                SecretKey = "secret",
                BucketName = "bucket",
            }),
            NullLogger<S3AvatarStorageService>.Instance,
            uploader);

    private sealed class StubDownloader(string? mediaType = null, bool throwOnDownload = false) : IFileDownloader
    {
        public async Task<string> DownloadAsync(Uri source, Stream target, long maxSize, CancellationToken ct)
        {
            if (throwOnDownload)
            {
                throw new FileDownloadException("Файл слишком большой.");
            }
            await target.WriteAsync(new byte[] { 1, 2, 3 }, ct);
            return mediaType!;
        }
    }

    private sealed class StubUploader(bool throwOnUpload = false) : IFileS3Uploader
    {
        public string? UploadedKey { get; private set; }
        public string? UploadedContentType { get; private set; }

        public Task UploadFileAsync(string key, Stream content, string contentType, CancellationToken ct)
        {
            if (throwOnUpload)
            {
                throw new InvalidOperationException("S3 unavailable");
            }
            UploadedKey = key;
            UploadedContentType = contentType;
            return Task.CompletedTask;
        }
    }
}
