namespace JoinRpg.DomainTypes.Users;

/// <summary>
/// Выбранная аватарка пользователя, как она хранится. Адрес для показа из неё строит <see cref="IAvatarUriResolver"/>.
/// </summary>
/// <param name="CachedUri">Ключ закешированной копии в хранилище (у старых записей — полный адрес).</param>
/// <param name="OriginalUri">Адрес у провайдера.</param>
public record UserAvatarSource(AvatarIdentification Id, string? CachedUri, string OriginalUri);
