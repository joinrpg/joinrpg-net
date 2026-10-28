namespace JoinRpg.Interfaces;

/// <summary>
/// Загружает аватарку пользователя: URI картинки и её размер.
/// </summary>
/// <remarks>
/// Реализация кеширует результат по паре (id аватарки, размер): аватарки неизменяемы,
/// поэтому повторно ходить за ними в БД не нужно.
/// </remarks>
public interface IAvatarLoader
{
    /// <summary>
    /// Возвращает аватарку <paramref name="userAvatarId"/>, подходящую под рекомендуемый размер <paramref name="recommendedSize"/>.
    /// </summary>
    Task<AvatarInfo> GetAvatar(AvatarIdentification userAvatarId, int recommendedSize);
}
