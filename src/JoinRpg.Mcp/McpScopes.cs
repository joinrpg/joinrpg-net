namespace JoinRpg.Mcp;

/// <summary>
/// Scope, которые принимает эндпоинт /mcp. Публикуются в Protected Resource Metadata
/// (RFC 9728 §2) — именно оттуда клиент узнаёт, что запрашивать у IdPortal.
/// </summary>
/// <remarks>
/// Значения обязаны совпадать с <c>JoinRpgScopes</c> в <c>JoinRpg.IdPortal.OAuthServer</c>.
/// Общего кода между Portal и IdPortal нет — это контракт токена, как и claim
/// <c>projects</c> в <see cref="McpAuthContext"/>. Если строк станет больше, стоит вынести
/// их в общий проект (например, JoinRpg.Common.WebInfrastructure, на который ссылаются обе
/// стороны), чтобы они не разъехались.
/// </remarks>
internal static class McpScopes
{
    public const string Read = "joinrpg.read";
    public const string CharactersWrite = "joinrpg.characters.write";

    public static readonly string[] All = [Read, CharactersWrite];
}
