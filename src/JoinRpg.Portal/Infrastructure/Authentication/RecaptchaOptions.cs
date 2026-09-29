namespace JoinRpg.Portal.Infrastructure.Authentication;

public class RecaptchaOptions
{
    public required string PublicKey { get; set; }
    public required string PrivateKey { get; set; }
}
