using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Primitives;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Заголовки X-Forwarded-*, какими они пришли до ForwardedHeadersMiddleware. Нужны для тегов
/// <see cref="PrivateNetworkRequestMetrics"/>.
/// </summary>
/// <param name="ForwardedForCount">Сколько адресов было в X-Forwarded-For (по всем значениям заголовка).</param>
/// <param name="ForwardedProto">X-Forwarded-Proto: <c>none</c>, <c>http</c>, <c>https</c> или <c>other</c>.</param>
internal readonly record struct IncomingForwardedHeaders(int ForwardedForCount, string ForwardedProto)
{
    /// <summary>
    /// Заголовков X-Forwarded-* не было.
    /// </summary>
    public static IncomingForwardedHeaders None { get; } = new(0, "none");

    /// <summary>
    /// Снять заголовки до ForwardedHeadersMiddleware.
    /// </summary>
    /// <remarks>
    /// После middleware по заголовкам это надёжно не восстановить: обработанные значения удаляются,
    /// а X-Original-* ставятся, только если у соединения был адрес (в TestServer его нет).
    /// </remarks>
    public static IncomingForwardedHeaders Capture(HttpRequest request)
        => new(
            CountEntries(request.Headers[ForwardedHeadersDefaults.XForwardedForHeaderName]),
            ClassifyProto(request.Headers[ForwardedHeadersDefaults.XForwardedProtoHeaderName]));

    /// <summary>
    /// Число непустых элементов списка через запятую по всем значениям заголовка.
    /// </summary>
    internal static int CountEntries(StringValues values) => EnumerateEntries(values).Count();

    /// <summary>
    /// Классифицирует X-Forwarded-Proto по ближайшему к нам (последнему) значению —
    /// именно его применяет ForwardedHeadersMiddleware при <c>ForwardLimit = 1</c>.
    /// </summary>
    internal static string ClassifyProto(StringValues values)
    {
        var last = EnumerateEntries(values).LastOrDefault();
        return last switch
        {
            null => "none",
            _ when last.Equals("https", StringComparison.OrdinalIgnoreCase) => "https",
            _ when last.Equals("http", StringComparison.OrdinalIgnoreCase) => "http",
            _ => "other",
        };
    }

    private static IEnumerable<string> EnumerateEntries(StringValues values)
    {
        foreach (var value in values)
        {
            if (value is null)
            {
                continue;
            }
            foreach (var entry in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                yield return entry;
            }
        }
    }
}
