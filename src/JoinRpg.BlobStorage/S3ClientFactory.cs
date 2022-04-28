using Amazon.S3;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.BlobStorage;
internal static class S3ClientFactory
{
    internal static IAmazonS3 CreateS3Client(S3StorageOptions options)
    {
        var config = new AmazonS3Config()
        {
            ServiceURL = options.Endpoint,

        };

        return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
    }
}
