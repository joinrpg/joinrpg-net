namespace JoinRpg.BlobStorage;

/// <summary>
/// Чем закончилась последняя настоящая загрузка аватарки в S3 в этой реплике.
/// <see cref="HealthCheckS3Storage"/> по <c>GetBucketLocation</c> видит только, что бакет доступен на чтение.
/// Права на запись так не проверить, а пробная запись из health check — это запись в S3 на каждый
/// внешний запрос к /health. Поэтому запись проверяется по настоящим загрузкам.
/// </summary>
internal sealed class S3UploadHealth
{
    private volatile string? lastFailureErrorCode;

    /// <summary>
    /// Код ошибки S3, если последняя загрузка не удалась; <c>null</c>, если удалась или загрузок ещё не было.
    /// </summary>
    public string? LastFailureErrorCode => lastFailureErrorCode;

    public void ReportSuccess() => lastFailureErrorCode = null;

    public void ReportFailure(string errorCode) => lastFailureErrorCode = errorCode;
}
