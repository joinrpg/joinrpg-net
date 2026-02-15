using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.BlobStorage;

public static class BlobStorageRegistration
{
    public static IServiceCollection AddJoinBlobStorage(this IServiceCollection services)
    {
        _ = services.AddHealthChecks()
            .AddCheck<HealthCheckS3Storage>("S3 storage");

        return services
            .AddHttpClient()
            .AddTransient<AvatarDownloader>()
            .AddTransient<IAvatarStorageService>(sp =>
        {
            var s3Options = sp.GetRequiredService<IOptions<S3StorageOptions>>();
            if (s3Options.Value.Configured)
            {
                return sp.GetRequiredService<S3AvatarStorageService>();
            }
            else
            {
                return sp.GetRequiredService<StubAvatarStorageService>();
            }
        })
            .AddTransient<S3AvatarStorageService>()
            .AddTransient<StubAvatarStorageService>()
            .AddSingleton<IAmazonS3>(sp => CreateS3Client(sp.GetRequiredService<IOptions<S3StorageOptions>>().Value));


    }

    internal static IAmazonS3 CreateS3Client(S3StorageOptions options)
    {
        var config = new AmazonS3Config()
        {
            ServiceURL = options.Endpoint,
            // https://github.com/aws/aws-sdk-net/issues/3610
            // Яндекс не поддерживает
            RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = Amazon.Runtime.ResponseChecksumValidation.WHEN_REQUIRED,
        };

        return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
    }
}
