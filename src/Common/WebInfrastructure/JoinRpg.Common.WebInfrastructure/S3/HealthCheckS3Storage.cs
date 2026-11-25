using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoinRpg.Common.WebInfrastructure.S3;

internal class HealthCheckS3Storage(IServiceProvider serviceProvider,
    ILogger<HealthCheckS3Storage> logger,
    IOptions<S3StorageOptions> options,
    S3UploadHealth uploadHealth) : IHealthCheck
{
    private readonly S3StorageOptions options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!options.Configured)
        {
            // Чек зарегистрирован у всех хостов платформы (AddJoinWebPlatform): без настроек S3
            // он не должен деградировать /health — S3 этому хосту просто не нужен.
            return HealthCheckResult.Healthy("Not configured");
        }

        // IAmazonS3 резолвится лениво: без настроек его создание падает (AmazonS3Client требует ServiceURL)
        var amazonS3 = serviceProvider.GetRequiredService<IAmazonS3>();
        var data = new Dictionary<string, object> { { "bucket", options.BucketName ?? "null" } };
        try
        {
            // AmazonS3Util.DoesS3BucketExistV2Async не принимает CancellationToken, поэтому
            // подвисший S3 отменить невозможно — вызываем API напрямую, с токеном.
            _ = await amazonS3.GetBucketLocationAsync(
                new GetBucketLocationRequest { BucketName = options.BucketName },
                cancellationToken);

            // GetBucketLocation не проверяет права на запись: без них бакет читается, а загрузки падают
            if (uploadHealth.LastFailureErrorCode is string errorCode)
            {
                data["lastUploadError"] = errorCode;
                return HealthCheckResult.Unhealthy($"Last file upload failed: {errorCode}", data: data);
            }

            return HealthCheckResult.Healthy(data: data);
        }
        catch (AmazonS3Exception exception) when (exception.ErrorCode is "NoSuchBucket" or "NotFound")
        {
            return HealthCheckResult.Degraded("Bucket does not exists", data: data);
        }
        catch (OperationCanceledException)
        {
            // Отмена — это сработавший таймаут чека или ушедший клиент. Пусть движок
            // health-чеков отчитается про таймаут, а не мы про «сломанный S3».
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error during connecting to S3 blob storage");
            return HealthCheckResult.Unhealthy(exception: exception, data: data);
        }
    }
}
