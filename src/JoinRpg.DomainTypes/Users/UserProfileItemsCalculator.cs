namespace JoinRpg.DomainTypes.Users;

/// <summary>
/// Вычисляет, какие элементы профиля пользователя не заполнены. Не знает о настройках проекта —
/// только о самих фактах (есть телеграм/вк/телефон/ФИО или нет). Используется и для полноценного
/// <see cref="UserInfo"/>, и там, где нет возможности собрать его целиком (напр. фильтры проблем
/// заявки, работающие напрямую с EF-сущностями).
/// </summary>
public static class UserProfileItemsCalculator
{
    /// <summary>
    /// Минимальная длина значения телефона/ФИО, при которой оно считается заполненным.
    /// </summary>
    public const int MinContactLength = 5;

    public static bool IsCorrectContact(string? value) => (value?.Length ?? 0) >= MinContactLength;

    /// <summary>
    /// Перегрузка на "сырых" фактах — нужна тем потребителям, которые не могут дёшево собрать
    /// полноценный <see cref="UserInfo"/> (напр. фильтры проблем заявки, работающие напрямую с
    /// EF-сущностями, минуя UserInfo). Когда такие потребители переедут на UserInfo, этот
    /// оверлоад можно будет убрать и оставить только <see cref="UserInfo.GetMissingItems"/>.
    /// </summary>
    public static IReadOnlyCollection<UserProfileItemType> GetMissingItems(
        bool hasTelegram,
        bool hasVerifiedVkontakte,
        string? phoneNumber,
        string? fullName)
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

        return missing;
    }
}
