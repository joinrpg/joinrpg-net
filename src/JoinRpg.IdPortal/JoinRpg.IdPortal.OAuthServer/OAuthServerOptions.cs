namespace JoinRpg.IdPortal.OAuthServer;

public class OAuthServerOptions
{
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

    public OAuthServerCertificatesOptions? Certificates { get; set; }
}
