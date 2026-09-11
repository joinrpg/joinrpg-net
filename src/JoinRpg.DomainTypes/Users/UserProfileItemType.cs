namespace JoinRpg.DomainTypes.Users;

/// <summary>
/// Элемент профиля пользователя, заполненность которого может требоваться проектом.
/// </summary>
public enum UserProfileItemType
{
    Telegram,
    Vkontakte,
    Phone,
    RealName,
    Passport,
    RegistrationAddress,
}
