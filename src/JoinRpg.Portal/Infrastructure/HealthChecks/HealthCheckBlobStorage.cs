using JoinRpg.BlobStorage;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Infrastructure.HealthChecks;

public class HealthCheckBlobStorage : IHealthCheck
{
    private readonly AzureBlobStorageConnectionFactory connectionFactory;
    private readonly ILogger<HealthCheckBlobStorage> logger;
    private readonly BlobStorageOptions blobStorageOptions;

    public HealthCheckBlobStorage(AzureBlobStorageConnectionFactory connectionFactory,
        ILogger<HealthCheckBlobStorage> logger,
        IOptions<BlobStorageOptions> blobStorageOptions)
    {
        this.connectionFactory = connectionFactory;
        this.logger = logger;
        this.blobStorageOptions = blobStorageOptions.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!blobStorageOptions.BlobStorageConfigured)
        {
            return HealthCheckResult.Degraded("Blob storage not configured");
        }

        try
        {
            var x = await connectionFactory.ConnectToAzureAsync("health-check-container", cancellationToken);
            return HealthCheckResult.Healthy(data: new Dictionary<string, object> { { "uri", x.Uri } });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error during connecting to Azure blob storage");
            return HealthCheckResult.Unhealthy(exception: exception);
        }
    }
}
