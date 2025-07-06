using Amazon.S3.Util;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JoinRpg.BlobStorage;

public class HealthCheckS3Storage(IAmazonS3 amazonS3,
    ILogger<HealthCheckS3Storage> logger,
    IOptions<S3StorageOptions> options) : IHealthCheck
{
    private readonly S3StorageOptions options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!options.Configured)
        {
            return HealthCheckResult.Degraded("Not configured");
        }
        var data = new Dictionary<string, object> { { "bucket", options.BucketName ?? "null" } };
        try
        {
            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(amazonS3, options.BucketName);
            if (exists)
            {
                return HealthCheckResult.Healthy(data: data);
            }
            else
            {

                return HealthCheckResult.Degraded("Bucket does not exists", data: data);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error during connecting to S3 blob storage");
            return HealthCheckResult.Unhealthy(exception: exception, data: data);
        }
    }
}
