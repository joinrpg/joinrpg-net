using System.Buffers;
using Microsoft.Extensions.Logging;

namespace JoinRpg.BlobStorage;
internal class AvatarDownloader
{
    private readonly ILogger<AvatarDownloader> logger;
    private readonly IHttpClientFactory httpClientFactory;

    public AvatarDownloader(IHttpClientFactory httpClientFactory, ILogger<AvatarDownloader> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    internal async Task<(string ContentType, string Extension)> DownloadAvatarAsync(Uri remoteUri, Stream target, CancellationToken ct)
    {
        logger.LogInformation("Start downloading avatar for {avatarRemoteUri}", remoteUri);

        var httpClient = httpClientFactory.CreateClient("download-avatar-client");

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
            _ => null,
        };
    }
}
