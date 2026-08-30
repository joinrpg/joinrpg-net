namespace JoinRpg.IdPortal.OAuthServer;

/// <summary>
/// Whether an OAuth client can hold a secret securely (server-side web app) or not (native/CLI app,
/// relies on PKCE instead of a client secret).
/// </summary>
public enum OAuthClientType
{
    Confidential,
    Public,
}
