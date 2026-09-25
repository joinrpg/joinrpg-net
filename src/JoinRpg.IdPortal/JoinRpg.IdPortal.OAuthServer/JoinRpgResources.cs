using JoinRpg.Common.WebInfrastructure;

namespace JoinRpg.IdPortal.OAuthServer;

/// <summary>
/// Ресурсы (audience) в терминах RFC 8707, на которые IdPortal выписывает токены.
/// </summary>
public static class JoinRpgResources
{
    /// <summary>
    /// Эндпоинт <c>/mcp</c> основного сайта — единственный ресурс для joinrpg.*-токенов.
    /// Значение обязано совпадать с тем, что Portal требует в <c>AddAudiences</c>, и с
    /// <c>resource</c> в его Protected Resource Metadata: MCP-клиент присылает именно его.
    /// </summary>
    public static string Mcp(JoinRpgHostNamesOptions hostNames) => $"https://{hostNames.MainHost}/mcp";
}
