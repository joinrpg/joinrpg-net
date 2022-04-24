using System.Security.Cryptography;
using System.Text;
using Azure.Storage.Blobs.Models;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace JoinRpg.BlobStorage;

/// <summary>
/// Service that allows to store blob to Azure
/// </summary>
internal class AzureAvatarStorageService : IAvatarStorageService
{
    private readonly ILogger<AzureAvatarStorageService> logger;
    private readonly AzureBlobStorageConnectionFactory connectionFactory;
    private readonly AvatarDownloader avatarDownloader;
    private readonly Random random = new();

    /// <summary>
    /// ctor
    /// </summary>
    public AzureAvatarStorageService(
        ILogger<AzureAvatarStorageService> logger,
        AzureBlobStorageConnectionFactory connectionFactory,
        AvatarDownloader avatarDownloader)
    {
        this.logger = logger;
        this.connectionFactory = connectionFactory;
        this.avatarDownloader = avatarDownloader;
    }
    async Task<Uri?> IAvatarStorageService.StoreAvatar(
        Uri remoteUri,
        CancellationToken ct)
    {
        var downloadAvatarTask = avatarDownloader.DownloadAvatarAsync(remoteUri, ct);
        var blobContainerClient = await connectionFactory.ConnectToAzureAsync("avatars", ct);
        var downloadResult = await downloadAvatarTask;

        var blobClient = blobContainerClient.GetBlobClient(CreateAvatarBlobName(remoteUri, downloadResult.Extension));

        _ = await blobClient.UploadAsync(
            downloadResult.Stream,
            new BlobHttpHeaders { ContentType = downloadResult.ContentType },
            cancellationToken: ct);

        logger.LogInformation("Avatar successfully uploaded to {avatarAzureUri}", blobClient.Uri);
        return blobClient.Uri;
    }


    private string CreateAvatarBlobName(Uri remoteUri, string extension)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(remoteUri.AbsoluteUri + random.Next().ToString("X")));
        return BitConverter.ToString(hash).Replace("-", "") + extension;
    }
}
