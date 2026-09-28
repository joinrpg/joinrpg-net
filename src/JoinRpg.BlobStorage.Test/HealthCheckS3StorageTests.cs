using Amazon.S3;
using Amazon.S3.Model;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace JoinRpg.BlobStorage;

/// <summary>
/// Регрессия на #4994: чек ходил в S3 через AmazonS3Util.DoesS3BucketExistV2Async, который не
/// принимает CancellationToken, поэтому подвисший S3 нельзя было отменить по таймауту.
/// </summary>
public class HealthCheckS3StorageTests
{
    [Fact]
    public async Task HangingS3IsCancelledByToken()
    {
        using var client = new StubS3Client(_ => Task.Delay(Timeout.Infinite, CancellationToken.None));
        var check = CreateCheck(client);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => check.CheckHealthAsync(new HealthCheckContext(), cts.Token));
    }

    [Fact]
    public async Task ExistingBucketIsHealthy()
    {
        using var client = new StubS3Client(_ => Task.CompletedTask);

        var result = await CreateCheck(client).CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task MissingBucketIsDegraded()
    {
        using var client = new StubS3Client(
            _ => throw new AmazonS3Exception("no bucket") { ErrorCode = "NoSuchBucket" });

        var result = await CreateCheck(client).CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task BrokenS3IsUnhealthy()
    {
        using var client = new StubS3Client(_ => throw new AmazonS3Exception("boom"));

        var result = await CreateCheck(client).CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    private static HealthCheckS3Storage CreateCheck(IAmazonS3 client)
        => new(client,
            NullLogger<HealthCheckS3Storage>.Instance,
            Options.Create(new S3StorageOptions
            {
                Endpoint = "https://s3.example.invalid",
                AccessKey = "key",
                SecretKey = "secret",
                BucketName = "bucket",
            }));

    /// <summary>
    /// Настоящий клиент AWS SDK с подменённым единственным вызовом, который делает чек.
    /// Реализовывать IAmazonS3 целиком ради одного метода смысла нет.
    /// </summary>
    private sealed class StubS3Client(Func<CancellationToken, Task> behavior)
        : AmazonS3Client("key", "secret", new AmazonS3Config { ServiceURL = "https://s3.example.invalid" })
    {
        public override async Task<GetBucketLocationResponse> GetBucketLocationAsync(
            GetBucketLocationRequest request,
            CancellationToken cancellationToken = default)
        {
            await behavior(cancellationToken).WaitAsync(cancellationToken);
            return new GetBucketLocationResponse();
        }
    }
}
