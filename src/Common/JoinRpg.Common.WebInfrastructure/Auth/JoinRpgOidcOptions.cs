namespace JoinRpg.Common.WebInfrastructure.Auth;

/// <summary>
/// Настройки OIDC-клиента для логина через id.joinrpg.ru. Читаются из секции конфигурации <c>JoinRpgOidc</c>.
/// </summary>
public class JoinRpgOidcOptions
{
    public Uri Issuer { get; set; } = new("https://id.joinrpg.ru/");
    public required string ClientId { get; set; }
    public string? ClientSecret { get; set; }

    /// <summary>
    /// При <see langword="true"/> на логине дополнительно дёргается <c>{Issuer}connect/user_info</c>,
    /// и полученные claims мёржатся в principal (best effort).
    /// </summary>
    public bool FetchUserInfo { get; set; } = false;
}
