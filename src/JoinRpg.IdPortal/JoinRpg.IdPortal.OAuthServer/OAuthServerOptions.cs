namespace JoinRpg.IdPortal.OAuthServer;

public class OAuthServerOptions
{
    public class OAuthServerRegistrationOptions
    {
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }

        public required Uri RedirectUri { get; set; }
    }

    public required OAuthServerRegistrationOptions[] Registrations { get; set; }
}

