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

    internal async Task<(Stream Stream, string ContentType, string Extension)> DownloadAvatarAsync(Uri remoteUri, CancellationToken ct)
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
}
