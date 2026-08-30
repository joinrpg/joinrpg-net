namespace JoinRpg.IdPortal.OAuthServer;

/// <summary>
/// OAuth scopes for MCP/LLM clients (ADR012), as opposed to the standard OIDC scopes.
/// </summary>
public static class JoinRpgScopes
{
    public const string Read = "joinrpg.read";
    public const string CharactersWrite = "joinrpg.characters.write";

    public static bool IsJoinRpgScope(string scope) => scope.StartsWith("joinrpg.", StringComparison.Ordinal);
}
