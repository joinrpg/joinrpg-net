using System.Buffers;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Common.WebInfrastructure.Files;

internal class FileDownloader(IHttpClientFactory httpClientFactory, ILogger<FileDownloader> logger) : IFileDownloader
{
    /// <summary>
    /// Имя именованного <see cref="HttpClient"/> для скачивания файлов.
    /// Регистрируется в <c>AmazonS3Registration.AddJoinS3Storage</c>;
    /// константа нужна, чтобы регистрация и использование не разъехались по опечатке
    /// (именно так появился баг #4995: клиента с таким именем не существовало, и
    /// фабрика молча отдавала дефолтный с таймаутом 100 секунд).
    /// </summary>
    internal const string HttpClientName = "download-file-client";

    /// <summary>
    /// Таймаут на скачивание файла. Размер ограничен вызывающим кодом (для аватарок — 1 МБ),
    /// поэтому дольше ждать смысла нет: пользователь стоит и ждёт ответа, например, на редирект логина.
    /// </summary>
    internal static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(10);

    public async Task<string> DownloadAsync(Uri source, Stream target, long maxSize, CancellationToken ct)
    {
        logger.LogInformation("Start downloading file from {sourceUri}", source);

        var httpClient = httpClientFactory.CreateClient(HttpClientName);

        var response = await httpClient.GetAsync(source, ct);

        var mediaType = (response.Content.Headers.ContentType?.MediaType) ?? throw new FileDownloadException("Downloaded file should have media type");

        long totalRead = 0;

        using var stream = await response.Content.ReadAsStreamAsync(ct);

        var buffer = ArrayPool<byte>.Shared.Rent(8192);

        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            totalRead += read;
            if (totalRead > maxSize)
            {
                throw new FileDownloadException("Файл слишком большой.");
            }
            target.Write(buffer, 0, read);
        }

        ArrayPool<byte>.Shared.Return(buffer);

        return mediaType;
    }
}
