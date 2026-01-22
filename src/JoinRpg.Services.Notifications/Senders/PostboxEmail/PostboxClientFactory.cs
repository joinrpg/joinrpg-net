using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Notifications.Senders.PostboxEmail;

internal class PostboxClientFactory(IOptions<PostboxOptions> options)
{
    private readonly Lazy<IAmazonSimpleEmailServiceV2> client = new Lazy<IAmazonSimpleEmailServiceV2>(() => CreateClient(options.Value));

    public IAmazonSimpleEmailServiceV2 Get() => client.Value;
    internal static IAmazonSimpleEmailServiceV2 CreateClient(PostboxOptions options)
    {
        var config = new AmazonSimpleEmailServiceV2Config()
        {
            ServiceURL = options.Endpoint,
            // https://github.com/aws/aws-sdk-net/issues/3610
            // Яндекс не поддерживает
            RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = Amazon.Runtime.ResponseChecksumValidation.WHEN_REQUIRED,
            SignatureMethod = SigningAlgorithm.HmacSHA256,
            SignatureVersion = "4",
            AuthenticationRegion = "ru-central1",
        };

        return new AmazonSimpleEmailServiceV2Client(options.AccessKey, options.SecretKey, config);
    }
}
