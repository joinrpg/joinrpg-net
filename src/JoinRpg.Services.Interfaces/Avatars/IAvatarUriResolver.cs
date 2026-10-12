namespace JoinRpg.Services.Interfaces.Avatars;

/// <summary>
/// Строит публичный адрес аватарки. В БД хранится ключ объекта в хранилище, домен берётся из настроек.
/// </summary>
public interface IAvatarUriResolver
{
    /// <summary>
    /// Адрес аватарки для показа
    /// </summary>
    /// <param name="cachedUri">Ключ объекта в хранилище (у старых записей — полный адрес)</param>
    /// <param name="originalUri">Адрес у провайдера — если закешированной копии нет</param>
    Uri GetAvatarUri(string? cachedUri, string originalUri);
}
