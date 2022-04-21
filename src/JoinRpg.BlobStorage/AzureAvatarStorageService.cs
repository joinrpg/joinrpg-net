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
    private readonly IHttpClientFactory httpClientFactory;
    private readonly AzureBlobStorageConnectionFactory connectionFactory;
    private readonly Random random = new();

    /// <summary>
    /// ctor
    /// </summary>
    public AzureAvatarStorageService(
        ILogger<AzureAvatarStorageService> logger,
        IHttpClientFactory httpClientFactory,
        AzureBlobStorageConnectionFactory connectionFactory)
    {
        this.logger = logger;
        this.httpClientFactory = httpClientFactory;
        this.connectionFactory = connectionFactory;
    }
    async Task<Uri?> IAvatarStorageService.StoreAvatar(
        Uri remoteUri,
        CancellationToken ct)
    {
        var downloadAvatarTask = DownloadAvatarAsync(remoteUri, ct);
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

    private async Task<(Stream Stream, string ContentType, string Extension)> DownloadAvatarAsync(Uri remoteUri, CancellationToken ct)
    {
        logger.LogInformation("Start downloading avatar for {avatarRemoteUri}", remoteUri);
        // TODO: We need to check size here to prevent downloading of something too large
        // but it's safe for now because remoteUri in practice only from "trusted sources" 
        // (100% avatar from social networks)
        var httpClient = httpClientFactory.CreateClient("download-avatar-client");

        var response = await httpClient.GetAsync(remoteUri, ct);
        var mediaType = response.Content.Headers.ContentType?.MediaType;

        if (mediaType is null)
        {
            throw new Exception("Avatar should have media type");
        }

        var extension = ParseContentTypeToExtension(mediaType);
        if (extension is null)
        {
            throw new Exception($"Is not safe to use {mediaType} as avatar media type");
        }

        return (await response.Content.ReadAsStreamAsync(ct), mediaType, extension);
    }

    private static string? ParseContentTypeToExtension(string mediaType)
    {
        return mediaType switch
        {
            "image/jpeg" => ".jpeg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/avif" => ".avif",
            _ => null,
        };
    }

    private string CreateAvatarBlobName(Uri remoteUri, string extension)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(remoteUri.AbsoluteUri + random.Next().ToString("X")));
        return BitConverter.ToString(hash).Replace("-", "") + extension;
    }
}
