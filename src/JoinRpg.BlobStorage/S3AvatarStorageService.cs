using System.Security.Cryptography;
using System.Text;
using JoinRpg.Common.WebInfrastructure.Files;

namespace JoinRpg.BlobStorage;

internal class S3AvatarStorageService(IFileDownloader downloader,
    IOptions<S3StorageOptions> options,
    ILogger<S3AvatarStorageService> logger,
    IFileS3Uploader uploader) : IAvatarStorageService
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
            logger.LogInformation("Start uploading avatar from {avatarUri}", remoteUri);

            using var memoryStream = new MemoryStream(8192);
            var contentType = await downloader.DownloadAsync(remoteUri, memoryStream, MaxAvatarSize, ct);

            if (ParseContentTypeToExtension(contentType) is not string extension)
            {
                logger.LogWarning("Не удалось закешировать аватарку с {avatarUri}: неподходящий media type {mediaType}", remoteUri, contentType);
                return null;
            }

            var avatarName = CreateAvatarBlobName(remoteUri, extension);

            await uploader.UploadFileAsync(avatarName, memoryStream, contentType, ct);

            var uri = new Uri($"{options.Endpoint}/{options.BucketName}/{avatarName}");

            logger.LogInformation(
                "Avatar uploaded to {cachedAvatarUri}",
                uri);

            return uri;
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
