using System.Security.Cryptography;
using System.Text;
using Amazon.S3.Model;
using JoinRpg.Common.WebInfrastructure.Files;

namespace JoinRpg.BlobStorage;

internal class S3AvatarStorageService(IFileDownloader downloader,
    IOptions<S3StorageOptions> options,
    ILogger<S3AvatarStorageService> logger,
    IAmazonS3 amazonS3client) : IAvatarStorageService
{
    /// <summary>
    /// Размер аватарки ограничен 1 МБ: пользователь стоит и ждёт ответа, например, на редирект логина.
    /// </summary>
    private const long MaxAvatarSize = 1 * 1024 * 1024;

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
                var contentType = await downloader.DownloadAsync(remoteUri, memoryStream, MaxAvatarSize, ct);

                if (ParseContentTypeToExtension(contentType) is not string extension)
                {
                    logger.LogWarning("Не удалось закешировать аватарку с {avatarUri}: неподходящий media type {mediaType}", remoteUri, contentType);
                    return null;
                }

                var avatarName = CreateAvatarBlobName(remoteUri, extension);

                var hash = SHA256.HashData(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);


                var putRequest1 = new PutObjectRequest
                {
                    BucketName = options.BucketName,
                    Key = avatarName,
                    InputStream = memoryStream,
                    ContentType = contentType,
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
        catch (FileDownloadException exception)
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

    private static string? ParseContentTypeToExtension(string mediaType)
    {
        return mediaType switch
        {
            "image/jpeg" => ".jpeg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/avif" => ".avif",
            "image/gif" => ".gif",
            _ => null,
        };
    }
}
