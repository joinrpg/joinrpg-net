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
        using var client = new StubS3Client(_ =>
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

    /// <summary>
    /// Регрессия: первая попытка PutObject могла дочитать стрим до конца (сбой после отправки тела).
    /// Ретрай обязан отправить файл целиком, а не хвост с текущей позиции.
    /// </summary>
    [Fact]
    public async Task RetryAfterPartiallyReadStreamSendsWholeFile()
    {
        var attempts = 0;
        byte[]? secondAttemptBody = null;
        using var client = new StubS3Client(request =>
        {
            attempts++;
            if (attempts == 1)
            {
                // Первая попытка «успела» прочитать стрим до конца и упала
                _ = request.InputStream.ReadByte();
                while (request.InputStream.ReadByte() >= 0)
                {
                }
                throw new AmazonS3Exception("connection reset") { ErrorCode = "InternalError" };
            }
            using var body = new MemoryStream();
            request.InputStream.CopyTo(body);
            secondAttemptBody = body.ToArray();
        });
        var uploadHealth = new S3UploadHealth();

        await CreateUploader(client, uploadHealth).UploadFileAsync("key", new MemoryStream([1, 2, 3]), "image/jpeg", CancellationToken.None);

        attempts.ShouldBe(2);
        secondAttemptBody.ShouldBe(new byte[] { 1, 2, 3 });
        uploadHealth.LastFailureErrorCode.ShouldBeNull();
    }

    [Fact]
    public async Task GivesUpAfterThreeRetries()
    {
        var attempts = 0;
        using var client = new StubS3Client(_ =>
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
        using var client = new StubS3Client(_ => { });
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

    private sealed class StubS3Client(Action<PutObjectRequest> behavior)
        : AmazonS3Client("key", "secret", new AmazonS3Config { ServiceURL = "https://s3.example.invalid" })
    {
        public override Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
        {
            behavior(request);
            return Task.FromResult(new PutObjectResponse());
        }
    }
}
