using System.Buffers;

namespace JoinRpg.BlobStorage;

internal class AvatarDownloader(IHttpClientFactory httpClientFactory, ILogger<AvatarDownloader> logger)
{
    /// <summary>
    /// Имя именованного <see cref="HttpClient"/> для скачивания аватарок.
    /// Регистрируется в <see cref="BlobStorageRegistration.AddJoinBlobStorage"/>;
    /// константа нужна, чтобы регистрация и использование не разъехались по опечатке
    /// (именно так появился баг #4995: клиента с таким именем не существовало, и
    /// фабрика молча отдавала дефолтный с таймаутом 100 секунд).
    /// </summary>
    internal const string HttpClientName = "download-avatar-client";

    /// <summary>
    /// Таймаут на скачивание аватарки. Размер аватарки ограничен 1 МБ (см. maxSize ниже),
    /// поэтому дольше ждать смысла нет: пользователь стоит и ждёт ответа на редирект логина.
    /// </summary>
    internal static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(10);

    internal async Task<(string ContentType, string Extension)> DownloadAvatarAsync(Uri remoteUri, Stream target, CancellationToken ct)
    {
        logger.LogInformation("Start downloading avatar for {avatarRemoteUri}", remoteUri);

        var httpClient = httpClientFactory.CreateClient(HttpClientName);

        var response = await httpClient.GetAsync(remoteUri, ct);

        var mediaType = (response.Content.Headers.ContentType?.MediaType) ?? throw new AvatarDownloadException("Avatar should have media type");
        var extension = ParseContentTypeToExtension(mediaType) ?? throw new AvatarDownloadException($"Is not safe to use {mediaType} as avatar media type");

        const long maxSize = 1 * 1024 * 1024; // 1 MB
        long totalRead = 0;

        using var stream = await response.Content.ReadAsStreamAsync(ct);

        var buffer = ArrayPool<byte>.Shared.Rent(8192);

        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            totalRead += read;
            if (totalRead > maxSize)
            {
                throw new InvalidOperationException("Файл слишком большой.");
            }
            target.Write(buffer, 0, read);
        }

        ArrayPool<byte>.Shared.Return(buffer);


        return (mediaType, extension);
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
