namespace Joinrpg.Web.Identity;

public class OAuthAuthenticationOptions
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    internal void Deconstruct(out string ClientId, out string ClientSecret)
    {
        ClientId = this.ClientId;
        ClientSecret = this.ClientSecret;
    }
}
