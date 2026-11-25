using Amazon.S3;
using JoinRpg.Common.WebInfrastructure.Files;
using JoinRpg.Common.WebInfrastructure.S3;
using Microsoft.Extensions.Options;

namespace JoinRpg.Common.WebInfrastructure;

public static class AmazonS3Registration
{
    /// <summary>
    /// Регистрирует S3-хранилище: клиент <see cref="IAmazonS3"/>, health check «S3 storage».
    /// При незаполненных настройках <see cref="S3StorageOptions"/> health check отвечает Healthy
    /// («Not configured») — хосты без S3 не получают деградацию /health.
    /// Вызывается из <c>AddJoinWebPlatform</c>.
    /// </summary>
    public static IServiceCollection AddJoinS3Storage(this IServiceCollection services)
    {
        _ = services.AddHealthChecks().AddCheck<HealthCheckS3Storage>("S3 storage");

        _ = services.AddHttpClient(FileDownloader.HttpClientName, client => client.Timeout = FileDownloader.DownloadTimeout);

        return services
            .AddTransient<IFileDownloader, FileDownloader>()
            .AddSingleton<S3UploadHealth>()
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
