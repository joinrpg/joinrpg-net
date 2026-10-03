namespace JoinRpg.DomainTypes.Users;

/// <summary>
/// Вычисляет, какие элементы профиля пользователя не заполнены. Не знает о настройках проекта —
/// только о самих фактах (есть телеграм/вк/телефон/ФИО или нет).
/// </summary>
public static class UserProfileItemsCalculator
{
    /// <summary>
    /// Минимальная длина значения телефона/ФИО, при которой оно считается заполненным.
    /// </summary>
    public const int MinContactLength = 5;

    public static bool IsCorrectContact(string? value) => (value?.Length ?? 0) >= MinContactLength;

    /// <summary>
    /// Перегрузка на "сырых" фактах. Заводилась под потребителей, которые не могли дёшево
    /// собрать полноценный <see cref="UserInfo"/> и читали контакты прямо из EF-сущностей;
    /// таких больше нет, и единственный вызывающий — <see cref="UserInfo.GetMissingItems"/>.
    /// То есть это уже просто тело того метода, вынесенное отдельно.
    /// </summary>
    public static IReadOnlyCollection<UserProfileItemType> GetMissingItems(
        bool hasTelegram,
        bool hasVerifiedVkontakte,
        string? phoneNumber,
        string? fullName,
        string? passportData = null,
        string? registrationAddress = null)
    {
        List<UserProfileItemType> missing = [];

        if (!hasTelegram)
        {
            missing.Add(UserProfileItemType.Telegram);
        }
        if (!hasVerifiedVkontakte)
        {
            missing.Add(UserProfileItemType.Vkontakte);
        }
        if (!IsCorrectContact(phoneNumber))
        {
            missing.Add(UserProfileItemType.Phone);
        }
        if (!IsCorrectContact(fullName))
        {
            missing.Add(UserProfileItemType.RealName);
        }
        if (!IsCorrectContact(passportData))
        {
            missing.Add(UserProfileItemType.Passport);
        }
        if (!IsCorrectContact(registrationAddress))
        {
            missing.Add(UserProfileItemType.RegistrationAddress);
        }

        return missing;
    }
}
