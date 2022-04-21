namespace JoinRpg.Web.XGameApi.Contract;

/// <summary>
/// Answer to auth attempt
/// </summary>
public class AuthenticationResponse
{
    /// <summary>
    /// Bearer token
    /// </summary>
    public string access_token { get; set; }
    /// <summary>
    /// Always same
    /// </summary>
    public string token_type { get; set; } = "bearer";

    /// <summary>
    /// In seconds
    /// </summary>
    public int expires_in { get; set; }
}
