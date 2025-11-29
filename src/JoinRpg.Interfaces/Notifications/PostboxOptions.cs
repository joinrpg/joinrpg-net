namespace JoinRpg.Interfaces.Notifications;
public class PostboxOptions
{
    public string? Endpoint { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }

    public bool Enabled { get; set; } = true;

    public bool Configured => !string.IsNullOrEmpty(Endpoint) && !string.IsNullOrEmpty(AccessKey) && !string.IsNullOrEmpty(SecretKey) && Enabled;
}
