namespace JoinRpg.Common.WebInfrastructure.Auth;

/// <summary>
/// Настройки OIDC-клиента для логина через id.joinrpg.ru. Читаются из секции конфигурации <c>JoinRpgOidc</c>.
/// </summary>
public class JoinRpgOidcOptions
{
    public Uri Issuer { get; set; } = new("https://id.joinrpg.ru/");
    public required string ClientId { get; set; }
    public string? ClientSecret { get; set; }
}
