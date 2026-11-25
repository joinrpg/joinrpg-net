namespace JoinRpg.Common.WebInfrastructure.S3;

/// <summary>
/// Загружает файлы в S3-хранилище. Реализация регистрируется через
/// <c>AddJoinS3Storage</c> в JoinRpg.Common.WebInfrastructure.
/// </summary>
public interface IFileS3Uploader
{
    /// <summary>
    /// Загружает <paramref name="content"/> в бакет под ключом <paramref name="key"/>.
    /// При сбоях S3 повторяет попытку (до 3 раз), после чего бросает исключение.
    /// Исход последней загрузки доступен health check'у «S3 storage».
    /// </summary>
    Task UploadFileAsync(string key, Stream content, string contentType, CancellationToken ct);
}
