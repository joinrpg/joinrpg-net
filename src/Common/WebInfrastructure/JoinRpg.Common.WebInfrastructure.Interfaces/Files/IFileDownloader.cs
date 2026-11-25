namespace JoinRpg.Common.WebInfrastructure.Files;

/// <summary>
/// Скачивает файл по URL в поток. Реализация регистрируется через
/// <c>AddJoinS3Storage</c> в JoinRpg.Common.WebInfrastructure.
/// </summary>
public interface IFileDownloader
{
    /// <summary>
    /// Скачивает <paramref name="source"/> в <paramref name="target"/>.
    /// Возвращает media type ответа.
    /// Бросает <see cref="FileDownloadException"/>, если у ответа нет media type
    /// или файл больше <paramref name="maxSize"/> байт.
    /// </summary>
    Task<string> DownloadAsync(Uri source, Stream target, long maxSize, CancellationToken ct);
}
