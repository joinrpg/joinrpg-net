using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Avatars;
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
        try
        {
            var retries = 0;
            while (true)
            {
                logger.LogInformation("Start uploading avatar from {avatarUri}", remoteUri);

                using var memoryStream = new MemoryStream(8192);
                var (contentType, extension) = await avatarDownloader.DownloadAvatarAsync(remoteUri, memoryStream, ct);

                var avatarName = CreateAvatarBlobName(remoteUri, extension);

                var hash = SHA256.HashData(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);


                var putRequest1 = new PutObjectRequest
                {
                    BucketName = options.BucketName,
                    Key = avatarName,
                    InputStream = memoryStream,
                    ContentType = contentType,
                    ChecksumSHA256 = Convert.ToBase64String(hash),
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
        catch (AvatarDownloadException exception)
        {
            logger.LogWarning(exception, "Не удалось закешировать аватарку с {avatarUri}", remoteUri);
            return null;
        }
    }

    private static string CreateAvatarBlobName(Uri remoteUri, string extension)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(remoteUri.AbsoluteUri + Random.Shared.Next().ToString("X")));
        return Convert.ToHexString(hash) + extension;
    }
}
