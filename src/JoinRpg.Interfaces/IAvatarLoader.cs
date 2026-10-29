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

    /// <summary>
    /// Возвращает аватарки <paramref name="avatarIds"/> пачкой — через тот же кеш, что и <see cref="GetAvatar"/>:
    /// то, что уже есть в кеше, в БД не запрашивается, а всё недостающее грузится одним запросом.
    /// </summary>
    /// <remarks>
    /// Повторы во входе схлопываются. Аватарки, которой нет в БД, в словаре не будет.
    /// </remarks>
    Task<IReadOnlyDictionary<AvatarIdentification, AvatarInfo>> GetAvatars(
        IReadOnlyCollection<AvatarIdentification> avatarIds,
        int recommendedSize);
}
