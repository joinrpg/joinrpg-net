namespace JoinRpg.IdPortal.OAuthServer;

public class OAuthServerOptions
{
    public class OAuthServerRegistrationOptions
    {
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }

        public required Uri[] RedirectUris { get; set; }
    }

    public class OAuthServerCertificateOptions
    {
        public string? Base64 { get; set; }
        public string? Password { get; set; }
    }

    public class OAuthServerCertificatesOptions
    {
        public OAuthServerCertificateOptions? Signing { get; set; }
        public OAuthServerCertificateOptions? Encryption { get; set; }
    }

    public required OAuthServerRegistrationOptions[] Registrations { get; set; }
    public OAuthServerCertificatesOptions? Certificates { get; set; }
}

