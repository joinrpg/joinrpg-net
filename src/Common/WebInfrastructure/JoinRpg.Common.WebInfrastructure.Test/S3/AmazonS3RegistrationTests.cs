using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure.S3;

/// <summary>
/// Чек «S3 storage» регистрируется всем хостам платформы (AddJoinWebPlatform), поэтому
/// при незаполненных настройках S3 он не должен деградировать /health.
/// </summary>
public class AmazonS3RegistrationTests
{
    [Fact]
    public async Task NotConfiguredIsHealthy()
    {
        var provider = new ServiceCollection().AddLogging().AddJoinS3Storage().BuildServiceProvider();

        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        report.Entries.ShouldContainKey("S3 storage");
        report.Entries["S3 storage"].Status.ShouldBe(HealthStatus.Healthy);
        report.Entries["S3 storage"].Description.ShouldBe("Not configured");
    }

    [Fact]
    public async Task ConfiguredRunsAgainstS3()
    {
        var services = new ServiceCollection().AddLogging()
            .Configure<S3StorageOptions>(o =>
            {
                o.Endpoint = "https://s3.example.invalid";
                o.AccessKey = "key";
                o.SecretKey = "secret";
                o.BucketName = "bucket";
            })
            .AddJoinS3Storage();
        // Подменяем клиента, чтобы чек не ходил в сеть (последняя регистрация побеждает)
        _ = services.AddSingleton<IAmazonS3>(new StubS3Client());
        var provider = services.BuildServiceProvider();

        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        report.Entries.ShouldContainKey("S3 storage");
        report.Entries["S3 storage"].Status.ShouldBe(HealthStatus.Healthy);
    }

    private sealed class StubS3Client()
        : AmazonS3Client("key", "secret", new AmazonS3Config { ServiceURL = "https://s3.example.invalid" })
    {
        public override Task<GetBucketLocationResponse> GetBucketLocationAsync(
            GetBucketLocationRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new GetBucketLocationResponse());
    }
}
