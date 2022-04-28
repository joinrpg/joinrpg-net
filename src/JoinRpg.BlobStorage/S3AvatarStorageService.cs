using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoinRpg.BlobStorage;

internal class S3AvatarStorageService : IAvatarStorageService
{
    private readonly AvatarDownloader avatarDownloader;
    private readonly S3StorageOptions options;
    private readonly ILogger<S3AvatarStorageService> logger;
    private readonly IAmazonS3 amazonS3Client;

    public S3AvatarStorageService(AvatarDownloader avatarDownloader,
        IOptions<S3StorageOptions> options,
        ILogger<S3AvatarStorageService> logger,
        IAmazonS3 amazonS3client)
    {
        this.avatarDownloader = avatarDownloader;
        this.options = options.Value;
        this.logger = logger;
        amazonS3Client = amazonS3client;
    }

    public async Task<Uri?> StoreAvatar(Uri remoteUri, CancellationToken ct = default)
    {
        logger.LogInformation("Start uploading avatar from {avatarUri}", remoteUri);
        var downloadResult = await avatarDownloader.DownloadAvatarAsync(remoteUri, ct);

        var avatarName = CreateAvatarBlobName(remoteUri, downloadResult.Extension);
        var putRequest1 = new PutObjectRequest
        {
            BucketName = options.BucketName,
            Key = avatarName,
            InputStream = downloadResult.Stream,
            ContentType = downloadResult.ContentType
        };

        _ = await amazonS3Client.PutObjectAsync(putRequest1, ct);
        var uri = new Uri($"{options.Endpoint}/{options.BucketName}/{avatarName}");

        logger.LogInformation(
            "Avatar uploaded to {cachedAvatarUri}",
            uri);

        return uri;
    }

    private static string CreateAvatarBlobName(Uri remoteUri, string extension)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(remoteUri.AbsoluteUri + Random.Shared.Next().ToString("X")));
        return BitConverter.ToString(hash).Replace("-", "") + extension;
    }
}
