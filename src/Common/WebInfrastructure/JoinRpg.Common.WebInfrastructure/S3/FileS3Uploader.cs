using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoinRpg.Common.WebInfrastructure.S3;

internal class FileS3Uploader(IAmazonS3 amazonS3client,
    IOptions<S3StorageOptions> options,
    ILogger<FileS3Uploader> logger,
    S3UploadHealth uploadHealth) : IFileS3Uploader
{
    private const int MaxRetries = 3;

    private readonly S3StorageOptions options = options.Value;

    public async Task UploadFileAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        var retries = 0;
        while (true)
        {
            if (content.CanSeek)
            {
                // Перед каждой попыткой, а не один раз: неудачная попытка могла
                // дочитать стрим до конца, и ретрай отправил бы хвост вместо файла
                content.Seek(0, SeekOrigin.Begin);
            }

            logger.LogInformation("Start uploading file {key} to S3 storage", key);

            var putRequest = new PutObjectRequest
            {
                BucketName = options.BucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType,
            };

            try
            {
                _ = await amazonS3client.PutObjectAsync(putRequest, ct);
            }
            catch (AmazonS3Exception s3exception)
            {
                if (retries < MaxRetries)
                {
                    logger.LogWarning(s3exception, "Проблема при загрузке файла {key} в S3 хранилище (Type={s3errorType}, Code={s3errorCode}: {message}", key, s3exception.ErrorType, s3exception.ErrorCode, s3exception.Message);
                    retries++;
                    continue;
                }
                logger.LogError(s3exception, "Проблема при загрузке файла {key} в S3 хранилище (Type={s3errorType}, Code={s3errorCode}: {message}", key, s3exception.ErrorType, s3exception.ErrorCode, s3exception.Message);
                uploadHealth.ReportFailure(s3exception.ErrorCode ?? s3exception.Message);
                throw;
            }
            uploadHealth.ReportSuccess();
            return;
        }
    }
}
