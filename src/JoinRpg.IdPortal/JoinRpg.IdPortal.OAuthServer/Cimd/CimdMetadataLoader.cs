using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JoinRpg.IdPortal.OAuthServer.Cimd;

public interface ICimdMetadataLoader
{
    Task<CimdDocument?> LoadAsync(CimdClientId clientId, CancellationToken ct = default);
}

/// <summary>
/// Скачивает и валидирует Client ID Metadata Document. Все ограничения — из §6 драфта:
/// только https, никаких приватных адресов, без редиректов, с таймаутом и лимитом размера.
/// </summary>
public sealed class CimdMetadataLoader(
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<CimdMetadataLoader> logger) : ICimdMetadataLoader
{
    /// <summary>§6.6: рекомендованный максимум — 5 КБ.</summary>
    internal const int MaxDocumentBytes = 5 * 1024;

    internal static readonly TimeSpan MinCacheLifetime = TimeSpan.FromMinutes(5);
    internal static readonly TimeSpan MaxCacheLifetime = TimeSpan.FromHours(24);

    public async Task<CimdDocument?> LoadAsync(CimdClientId clientId, CancellationToken ct = default)
    {
        var cacheKey = $"cimd:{clientId.Url}";
        if (cache.TryGetValue<CimdDocument>(cacheKey, out var cached))
        {
            return cached;
        }

        var (document, cacheFor) = await FetchAsync(clientId, ct);
        if (document is null)
        {
            // §4.4: ошибки и невалидные документы не кэшируем, иначе временный сбой у клиента
            // заморозил бы отказ на часы.
            return null;
        }

        cache.Set(cacheKey, document, cacheFor);
        return document;
    }

    private async Task<(CimdDocument? Document, TimeSpan CacheFor)> FetchAsync(
        CimdClientId clientId, CancellationToken ct)
    {
        if (!await CimdAddressGuard.IsHostAllowedAsync(clientId.Url.Host, ct))
        {
            logger.LogWarning(
                "CIMD: отказ, хост {Host} резолвится в приватный или loopback адрес", clientId.Host);
            return (null, default);
        }

        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, clientId.Url);
            request.Headers.Accept.ParseAdd("application/json");
            response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(e, "CIMD: не удалось скачать документ по {Url}", clientId.Url);
            return (null, default);
        }

        using (response)
        {
            // Редиректы выключены на уровне handler'а: 3xx приезжает сюда как обычный ответ.
            // Следовать за ними нельзя — это обход проверки адреса.
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("CIMD: {Url} ответил {Status}", clientId.Url, (int)response.StatusCode);
                return (null, default);
            }

            if (!IsJson(response.Content.Headers.ContentType?.MediaType))
            {
                logger.LogWarning("CIMD: {Url} вернул content-type {Type}, ожидался JSON",
                    clientId.Url, response.Content.Headers.ContentType?.MediaType);
                return (null, default);
            }

            var json = await ReadLimitedAsync(response, ct);
            if (json is null)
            {
                logger.LogWarning("CIMD: документ по {Url} превышает {Limit} байт", clientId.Url, MaxDocumentBytes);
                return (null, default);
            }

            var result = CimdDocument.Validate(json, clientId);
            if (!result.IsValid)
            {
                logger.LogWarning("CIMD: документ по {Url} невалиден — {Error}", clientId.Url, result.Error);
                return (null, default);
            }

            return (result.Document, ResolveCacheLifetime(response));
        }
    }

    private static bool IsJson(string? mediaType) =>
        mediaType is not null
        && (mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || (mediaType.StartsWith("application/", StringComparison.OrdinalIgnoreCase)
                && mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// Читает не больше <see cref="MaxDocumentBytes"/>, не доверяя Content-Length: сервер мог
    /// его не прислать или соврать. Больше лимита — отказ, а не усечение (усечённый JSON
    /// всё равно не распарсится, но отказ честнее).
    /// </summary>
    private static async Task<string?> ReadLimitedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.Content.Headers.ContentLength > MaxDocumentBytes)
        {
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        var buffer = new byte[MaxDocumentBytes + 1];
        var total = 0;

        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total), ct);
            if (read == 0)
            {
                break;
            }
            total += read;
        }

        return total > MaxDocumentBytes ? null : Encoding.UTF8.GetString(buffer, 0, total);
    }

    /// <summary>
    /// §4.4: уважаем HTTP-заголовки кэширования, но со своими границами — чтобы клиент не мог
    /// ни заставить нас ходить к нему на каждый запрос, ни залипнуть на устаревшем документе.
    /// </summary>
    private static TimeSpan ResolveCacheLifetime(HttpResponseMessage response)
    {
        var cacheControl = response.Headers.CacheControl;
        if (cacheControl is { NoStore: true } or { NoCache: true })
        {
            return MinCacheLifetime;
        }

        if (cacheControl?.MaxAge is { } maxAge)
        {
            return Clamp(maxAge);
        }

        if (response.Content.Headers.Expires is { } expires)
        {
            return Clamp(expires - DateTimeOffset.UtcNow);
        }

        return MinCacheLifetime;
    }

    private static TimeSpan Clamp(TimeSpan value) =>
        value < MinCacheLifetime ? MinCacheLifetime
        : value > MaxCacheLifetime ? MaxCacheLifetime
        : value;

    /// <summary>Настройка HttpClient: без редиректов и с коротким таймаутом.</summary>
    public static void ConfigureHttpClient(HttpClient client) => client.Timeout = TimeSpan.FromSeconds(5);

    public static HttpMessageHandler CreateHandler() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.All,
        ConnectTimeout = TimeSpan.FromSeconds(3),
    };
}
