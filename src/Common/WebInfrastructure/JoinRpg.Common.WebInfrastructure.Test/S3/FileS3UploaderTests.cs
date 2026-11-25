using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure.S3;

/// <summary>
/// Загрузка файла ретраится при сбоях S3 и сообщает свой исход в <see cref="S3UploadHealth"/>,
/// по которому health check видит, что запись в бакет не проходит.
/// </summary>
public class FileS3UploaderTests
{
    [Fact]
    public async Task RetrySucceedsOnSecondAttempt()
    {
        var attempts = 0;
        using var client = new StubS3Client(() =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new AmazonS3Exception("temporary") { ErrorCode = "SlowDown" };
            }
        });
        var uploadHealth = new S3UploadHealth();

        await CreateUploader(client, uploadHealth).UploadFileAsync("key", new MemoryStream([1, 2, 3]), "image/jpeg", CancellationToken.None);

        attempts.ShouldBe(2);
        uploadHealth.LastFailureErrorCode.ShouldBeNull();
    }

    [Fact]
    public async Task GivesUpAfterThreeRetries()
    {
        var attempts = 0;
        using var client = new StubS3Client(() =>
        {
            attempts++;
            throw new AmazonS3Exception("Access Denied") { ErrorCode = "AccessDenied" };
        });
        var uploadHealth = new S3UploadHealth();

        _ = await Should.ThrowAsync<AmazonS3Exception>(
            () => CreateUploader(client, uploadHealth).UploadFileAsync("key", new MemoryStream([1, 2, 3]), "image/jpeg", CancellationToken.None));

        attempts.ShouldBe(4); // первая попытка + 3 ретрая
        uploadHealth.LastFailureErrorCode.ShouldBe("AccessDenied");
    }

    [Fact]
    public async Task SuccessfulUploadClearsFailure()
    {
        using var client = new StubS3Client(() => { });
        var uploadHealth = new S3UploadHealth();
        uploadHealth.ReportFailure("AccessDenied");

        await CreateUploader(client, uploadHealth).UploadFileAsync("key", new MemoryStream([1, 2, 3]), "image/jpeg", CancellationToken.None);

        uploadHealth.LastFailureErrorCode.ShouldBeNull();
    }

    private static FileS3Uploader CreateUploader(IAmazonS3 client, S3UploadHealth uploadHealth)
        => new(client,
            Options.Create(new S3StorageOptions
            {
                Endpoint = "https://s3.example.invalid",
                AccessKey = "key",
                SecretKey = "secret",
                BucketName = "bucket",
            }),
            NullLogger<FileS3Uploader>.Instance,
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
}
