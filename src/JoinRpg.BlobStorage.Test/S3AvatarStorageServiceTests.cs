using System.Net;
using System.Net.Http.Headers;
using Amazon.S3;
using Amazon.S3.Model;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace JoinRpg.BlobStorage;

/// <summary>
/// Загрузка аватарки сообщает свой исход в <see cref="S3UploadHealth"/>, по которому health check
/// видит, что запись в бакет не проходит.
/// </summary>
public class S3AvatarStorageServiceTests
{
    private static readonly Uri RemoteUri = new("https://example.invalid/avatar.jpg");

    [Fact]
    public async Task DeniedUploadIsReported()
    {
        using var client = new StubS3Client(() => throw new AmazonS3Exception("Access Denied") { ErrorCode = "AccessDenied" });
        var uploadHealth = new S3UploadHealth();

        _ = await Should.ThrowAsync<AmazonS3Exception>(() => CreateService(client, uploadHealth).StoreAvatar(RemoteUri));

        uploadHealth.LastFailureErrorCode.ShouldBe("AccessDenied");
    }

    [Fact]
    public async Task SuccessfulUploadClearsFailure()
    {
        using var client = new StubS3Client(() => { });
        var uploadHealth = new S3UploadHealth();
        uploadHealth.ReportFailure("AccessDenied");

        _ = await CreateService(client, uploadHealth).StoreAvatar(RemoteUri);

        uploadHealth.LastFailureErrorCode.ShouldBeNull();
    }

    private static S3AvatarStorageService CreateService(IAmazonS3 client, S3UploadHealth uploadHealth)
        => new(new AvatarDownloader(new StubHttpClientFactory(), NullLogger<AvatarDownloader>.Instance),
            Options.Create(new S3StorageOptions
            {
                Endpoint = "https://s3.example.invalid",
                AccessKey = "key",
                SecretKey = "secret",
                BucketName = "bucket",
            }),
            NullLogger<S3AvatarStorageService>.Instance,
            client,
            uploadHealth);

    private sealed class StubS3Client(Action behavior)
        : AmazonS3Client("key", "secret", new AmazonS3Config { ServiceURL = "https://s3.example.invalid" })
    {
        public override Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
        {
            behavior();
            return Task.FromResult(new PutObjectResponse());
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler());
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
