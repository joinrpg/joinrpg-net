using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoinRpg.BlobStorage
{
    /// <summary>
    /// Connects to Azure blob storage
    /// </summary>
    public class AzureBlobStorageConnectionFactory
    {
        private readonly string connectionString;
        private readonly ILogger<AzureBlobStorageConnectionFactory> logger;

        /// <summary>ctor </summary>
        public AzureBlobStorageConnectionFactory(
            IOptions<BlobStorageOptions> options,
            ILogger<AzureBlobStorageConnectionFactory> logger)
        {
            connectionString = options.Value.BlobStorageConnectionString;
            this.logger = logger;
        }

        /// <summary>
        /// Create if not exists container and return client
        /// </summary>
        public async Task<BlobContainerClient> ConnectToAzureAsync(string containerName, CancellationToken ct)
        {
            var client = new BlobServiceClient(connectionString);
            var container = client.GetBlobContainerClient(containerName);
            if (!await container.ExistsAsync(ct))
            {
                logger.LogInformation("Container {blobContainerName} not exist, creating", containerName);
                _ = await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
            }

            return container;
        }
    }
}
