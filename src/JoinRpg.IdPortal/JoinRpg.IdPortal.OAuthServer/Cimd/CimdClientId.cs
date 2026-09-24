using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.IdPortal.OAuthServer.Cimd;

/// <summary>
/// client_id в виде HTTPS-URL (Client ID Metadata Document, ADR012 §3).
/// Разбор и проверка по §3
/// <see href="https://drafts.oauth.net/draft-ietf-oauth-client-id-metadata-document/draft-ietf-oauth-client-id-metadata-document.html">CIMD</see>
/// — без сети.
/// </summary>
public sealed class CimdClientId
{
    private CimdClientId(Uri url, string value)
    {
        Url = url;
        Value = value;
    }

    public Uri Url { get; }

    /// <summary>
    /// Исходная строка client_id, как её прислал клиент. Именно с ней сверяется поле
    /// <c>client_id</c> документа: §4.1 требует простого строкового сравнения, а
    /// <see cref="Uri"/> по дороге нормализует путь, регистр хоста и порт по умолчанию.
    /// </summary>
    public string Value { get; }

    /// <summary>Хост, который показывается пользователю на экране согласия.</summary>
    public string Host => Url.Host;

    /// <summary>
    /// Быстрая проверка «похоже ли это вообще на CIMD», чтобы не трогать обычные client_id.
    /// Намеренно грубая: всё остальное проверяет <see cref="TryParse"/>.
    /// </summary>
    public static bool LooksLikeCimd(string? clientId) =>
        clientId is not null
        && clientId.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    public static bool TryParse(string? clientId, [NotNullWhen(true)] out CimdClientId? result)
    {
        result = null;

        if (!LooksLikeCimd(clientId)
            || !Uri.TryCreate(clientId, UriKind.Absolute, out var url))
        {
            return false;
        }

        // §3: схема https; path обязателен; ни fragment, ни userinfo; query не рекомендован;
        // сегменты "." и ".." запрещены. Порт разрешён.
        if (!url.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.Ordinal)
            || url.AbsolutePath is "" or "/"
            || !string.IsNullOrEmpty(url.Fragment)
            || !string.IsNullOrEmpty(url.UserInfo)
            || !string.IsNullOrEmpty(url.Query)
            || HasDotSegments(clientId!))
        {
            return false;
        }

        result = new CimdClientId(url, clientId!);
        return true;
    }

    /// <summary>
    /// Ищет сегменты "." и ".." в ИСХОДНОЙ строке, а не в <see cref="Uri.AbsolutePath"/>:
    /// Uri схлопывает их при разборе, и "/a/../client.json" приехал бы сюда уже как
    /// "/client.json", то есть проверка никогда бы не срабатывала.
    /// </summary>
    private static bool HasDotSegments(string clientId)
    {
        var afterScheme = clientId.AsSpan("https://".Length);
        var slash = afterScheme.IndexOf('/');
        if (slash < 0)
        {
            return false;
        }

        var path = afterScheme[slash..].ToString();
        return path.Split('/').Any(segment => segment is "." or "..");
    }

    public override string ToString() => Value;
}
