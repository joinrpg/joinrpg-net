namespace JoinRpg.Mcp;

/// <summary>
/// Portal как OAuth resource server для /mcp (ADR012 §5): confidential-клиент IdPortal
/// с правом на connect/introspect, заведённый вручную через admin UI IdPortal.
/// </summary>
public class McpResourceOptions
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}
