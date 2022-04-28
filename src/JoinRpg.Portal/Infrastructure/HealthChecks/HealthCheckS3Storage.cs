using Amazon.S3;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Infrastructure.HealthChecks;

public class HealthCheckS3Storage : IHealthCheck
{
    private readonly IAmazonS3 amazonS3;
    private readonly ILogger<HealthCheckS3Storage> logger;
    private readonly S3StorageOptions options;

    public HealthCheckS3Storage(IAmazonS3 amazonS3,
        ILogger<HealthCheckS3Storage> logger,
        IOptions<S3StorageOptions> options)
    {
        this.amazonS3 = amazonS3;
        this.logger = logger;
        this.options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!options.Configured)
        {
            return HealthCheckResult.Degraded("Not configured");
        }
        var data = new Dictionary<string, object> { { "bucket", options.BucketName ?? "null" } };
        try
        {
            var exists = await amazonS3.DoesS3BucketExistAsync(options.BucketName);
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
            logger.LogError(exception, "Error during connecting to Azure blob storage");
            return HealthCheckResult.Unhealthy(exception: exception, data: data);
        }
    }
}
