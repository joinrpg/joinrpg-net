namespace JoinRpg.Services.Interfaces;
public class S3StorageOptions
{
    public string? Endpoint { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }

    public string? BucketName { get; set; }

    public bool Configured => !string.IsNullOrEmpty(Endpoint) && !string.IsNullOrEmpty(AccessKey) && !string.IsNullOrEmpty(SecretKey)
        && !string.IsNullOrWhiteSpace(BucketName);
}
