using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoinRpg.BlobStorage;

internal class S3AvatarStorageService(AvatarDownloader avatarDownloader,
    IOptions<S3StorageOptions> options,
    ILogger<S3AvatarStorageService> logger,
    IAmazonS3 amazonS3client) : IAvatarStorageService
{
    private readonly S3StorageOptions options = options.Value;

    public async Task<Uri?> StoreAvatar(Uri remoteUri, CancellationToken ct = default)
    {
        var retries = 0;
        while (true)
        {
            logger.LogInformation("Start uploading avatar from {avatarUri}", remoteUri);

            using var memoryStream = new MemoryStream(8192);
            var downloadResult = await avatarDownloader.DownloadAvatarAsync(remoteUri, memoryStream, ct);

            var avatarName = CreateAvatarBlobName(remoteUri, downloadResult.Extension);

            using var anotherStream = new MemoryStream([.. Enumerable.Repeat((byte)0, 8192)]);


            var putRequest1 = new PutObjectRequest
            {
                BucketName = options.BucketName,
                Key = avatarName,
                InputStream = anotherStream,
                ContentType = downloadResult.ContentType,
            };

            try
            {
                _ = await amazonS3client.PutObjectAsync(putRequest1, ct);
            }
            catch (AmazonS3Exception s3exception)
            {
                if (retries < 3)
                {
                    logger.LogWarning(s3exception, "Проблема при загрузке аватара {avatarUri} в S3 хранилище (Type={s3errorType}, Code={s3errorCode}: {message}", remoteUri, s3exception.ErrorType, s3exception.ErrorCode, s3exception.Message);
                    retries++;
                    continue;
                }
                logger.LogError(s3exception, "Проблема при загрузке аватара {avatarUri} в S3 хранилище (Type={s3errorType}, Code={s3errorCode}: {message}", remoteUri, s3exception.ErrorType, s3exception.ErrorCode, s3exception.Message);
                throw;
            }
            var uri = new Uri($"{options.Endpoint}/{options.BucketName}/{avatarName}");

            logger.LogInformation(
                "Avatar uploaded to {cachedAvatarUri}",
                uri);

            return uri;
        }
    }

    private static string CreateAvatarBlobName(Uri remoteUri, string extension)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(remoteUri.AbsoluteUri + Random.Shared.Next().ToString("X")));
        return Convert.ToHexString(hash) + extension;
    }
}
