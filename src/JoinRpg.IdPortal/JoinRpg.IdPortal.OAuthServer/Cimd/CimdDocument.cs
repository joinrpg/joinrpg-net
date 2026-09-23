using System.Text.Json;
using System.Text.Json.Serialization;

namespace JoinRpg.IdPortal.OAuthServer.Cimd;

/// <summary>
/// Client ID Metadata Document — то, что сервер скачивает по URL-образному client_id.
/// Поля из реестра OAuth Dynamic Client Registration Metadata; здесь только те, что нам нужны.
/// </summary>
public sealed class CimdDocument
{
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    [JsonPropertyName("client_uri")]
    public string? ClientUri { get; set; }

    [JsonPropertyName("redirect_uris")]
    public List<string>? RedirectUris { get; set; }

    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; set; }

    // Присутствие этих полей — повод отвергнуть документ целиком (§4.2 драфта).
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("client_secret_expires_at")]
    public long? ClientSecretExpiresAt { get; set; }

    /// <summary>
    /// Все redirect_uris ведут на loopback. Спека требует отдельно предупредить об этом
    /// пользователя: CIMD не защищает от подмены приложения на localhost.
    /// </summary>
    public bool IsLoopbackOnly =>
        ValidRedirectUris.Count > 0 && ValidRedirectUris.TrueForAll(uri => uri.IsLoopback);

    public List<Uri> ValidRedirectUris { get; private set; } = [];

    /// <summary>Методы клиентской аутентификации на общем симметричном секрете — запрещены (§4.2).</summary>
    private static readonly string[] ForbiddenAuthMethods =
        ["client_secret_post", "client_secret_basic", "client_secret_jwt"];

    public static CimdValidationResult Validate(string json, CimdClientId expectedClientId)
    {
        CimdDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<CimdDocument>(json);
        }
        catch (JsonException)
        {
            return CimdValidationResult.Invalid("документ не является валидным JSON");
        }

        if (document is null)
        {
            return CimdValidationResult.Invalid("документ пуст");
        }

        // §4.1: client_id в документе обязан совпадать с URL простым строковым сравнением
        // (RFC 3986 §6.2.1) — никакой нормализации.
        if (!string.Equals(document.ClientId, expectedClientId.Value, StringComparison.Ordinal))
        {
            return CimdValidationResult.Invalid(
                $"client_id в документе ('{document.ClientId}') не совпадает с URL, по которому он получен");
        }

        if (string.IsNullOrWhiteSpace(document.ClientName))
        {
            return CimdValidationResult.Invalid("отсутствует обязательное поле client_name");
        }

        if (document.ClientSecret is not null || document.ClientSecretExpiresAt is not null)
        {
            return CimdValidationResult.Invalid("документ не может содержать client_secret");
        }

        if (document.TokenEndpointAuthMethod is { } authMethod
            && ForbiddenAuthMethods.Contains(authMethod, StringComparer.Ordinal))
        {
            return CimdValidationResult.Invalid(
                $"token_endpoint_auth_method '{authMethod}' основан на общем секрете и запрещён");
        }

        if (document.RedirectUris is not { Count: > 0 })
        {
            return CimdValidationResult.Invalid("отсутствует обязательное поле redirect_uris");
        }

        foreach (var raw in document.RedirectUris)
        {
            if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
            {
                return CimdValidationResult.Invalid($"redirect_uri '{raw}' не является абсолютным URI");
            }

            // MCP security considerations: redirect_uri обязан быть либо localhost, либо https.
            if (uri.Scheme != Uri.UriSchemeHttps && !uri.IsLoopback)
            {
                return CimdValidationResult.Invalid(
                    $"redirect_uri '{raw}' должен использовать https или вести на loopback");
            }

            document.ValidRedirectUris.Add(uri);
        }

        return CimdValidationResult.Valid(document);
    }
}

public sealed record CimdValidationResult(CimdDocument? Document, string? Error)
{
    public bool IsValid => Document is not null;

    public static CimdValidationResult Valid(CimdDocument document) => new(document, null);

    public static CimdValidationResult Invalid(string error) => new(null, error);
}
