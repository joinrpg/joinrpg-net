namespace JoinRpg.DomainTypes.Users;

/// <summary>
/// Вычисляет, какие элементы профиля пользователя не заполнены. Не знает о настройках проекта —
/// только о самих фактах (есть телеграм/вк/телефон/ФИО или нет).
/// </summary>
internal static class UserProfileItemsCalculator
{
    /// <summary>
    /// Минимальная длина телефона, при которой он считается заполненным.
    /// </summary>
    private const int MinContactLength = 5;

    /// <summary>
    /// Минимальная длина паспортных данных/адреса регистрации, при которой они считаются
    /// заполненными. Серия с номером паспорта — уже 10 символов, поэтому общий порог контактов
    /// пропускал заведомо неполные значения.
    /// </summary>
    internal const int MinSensitiveDataLength = 20;

    private static bool IsCorrectContact(string? value) => (value?.Length ?? 0) >= MinContactLength;

    /// <summary>
    /// ФИО заполнено, только если указаны все три части: имя, фамилия и отчество.
    /// </summary>
    // TODO(#5780): дать человеку указать, что отчества у него нет, — тогда без него ФИО тоже заполнено.
    private static bool IsCorrectFullName(UserFullName fullName)
        => fullName is { BornName: not null, SurName: not null, FatherName: not null };

    private static bool IsCorrectSensitiveData(string? value) => (value?.Length ?? 0) >= MinSensitiveDataLength;

    /// <summary>
    /// Перегрузка на "сырых" фактах. Заводилась под потребителей, которые не могли дёшево
    /// собрать полноценный <see cref="UserInfo"/> и читали контакты прямо из EF-сущностей;
    /// таких больше нет, и единственный вызывающий — <see cref="UserInfo.GetMissingItems"/>.
    /// То есть это уже просто тело того метода, вынесенное отдельно, — потому и internal.
    /// </summary>
    internal static IReadOnlyCollection<UserProfileItemType> GetMissingItems(
        bool hasTelegram,
        bool hasVerifiedVkontakte,
        string? phoneNumber,
        UserFullName fullName,
        string? passportData = null,
        string? registrationAddress = null,
        DateOnly? birthDate = null)
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
        if (!IsCorrectFullName(fullName))
        {
            missing.Add(UserProfileItemType.RealName);
        }
        if (!IsCorrectSensitiveData(passportData))
        {
            missing.Add(UserProfileItemType.Passport);
        }
        if (!IsCorrectSensitiveData(registrationAddress))
        {
            missing.Add(UserProfileItemType.RegistrationAddress);
        }
        if (birthDate is null)
        {
            missing.Add(UserProfileItemType.BirthDate);
        }

        return missing;
    }
}
