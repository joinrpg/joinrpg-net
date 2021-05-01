using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JoinRpg.BlobStorage;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Portal.Infrastructure.HealthChecks
{
    public class HealthCheckBlobStorage : IHealthCheck
    {
        private readonly AzureBlobStorageConnectionFactory connectionFactory;
        private readonly ILogger<HealthCheckBlobStorage> logger;

        public HealthCheckBlobStorage(AzureBlobStorageConnectionFactory connectionFactory,
            ILogger<HealthCheckBlobStorage> logger)
        {
            this.connectionFactory = connectionFactory;
            this.logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
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
}
