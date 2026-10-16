using JoinRpg.Common.PrimitiveTypes;

namespace JoinRpg.Web.UserProfile;

/// <summary>
/// Аватарка пользователя для показа. Реализация есть только на сервере: <see cref="UserProfilePanel"/>
/// рендерится статически.
/// </summary>
public interface IUserAvatarSource
{
    /// <param name="selectedAvatarId">Выбранная пользователем аватарка; null — взять (или завести) аватарку по умолчанию.</param>
    Task<AvatarInfo> GetAvatar(UserIdentification userId, AvatarIdentification? selectedAvatarId, int recommendedSize);
}
