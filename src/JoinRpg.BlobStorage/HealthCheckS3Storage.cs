using Amazon.S3.Model;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JoinRpg.BlobStorage;

internal class HealthCheckS3Storage(IAmazonS3 amazonS3,
    ILogger<HealthCheckS3Storage> logger,
    IOptions<S3StorageOptions> options,
    S3UploadHealth uploadHealth) : IHealthCheck
{
    private readonly S3StorageOptions options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!options.Configured)
        {
            return HealthCheckResult.Degraded("Not configured");
        }
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
                return HealthCheckResult.Unhealthy($"Last avatar upload failed: {errorCode}", data: data);
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
