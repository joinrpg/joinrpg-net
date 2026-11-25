namespace JoinRpg.Common.WebInfrastructure.Files;

/// <summary>
/// Файл не удалось скачать: нет media type, превышен лимит размера и т.п.
/// </summary>
public class FileDownloadException(string message) : Exception(message);
