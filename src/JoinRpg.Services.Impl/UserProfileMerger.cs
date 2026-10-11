using JoinRpg.DataModel;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Слияние профиля при слиянии пользователей (ADR024): значение основного побеждает, пустые поля
/// основного заполняются из поглощаемого. Работает на сущностях, сохраняет вызывающий.
/// </summary>
public static class UserProfileMerger
{
    /// <returns>Названия полей, взятых у поглощаемого, — для отчёта админам.</returns>
    public static IReadOnlyList<string> MergeInto(User primary, User secondary)
    {
        var taken = new List<string>();

        // TODO[Localize] названия полей для отчёта
        primary.BornName = Pick(taken, "Имя", primary.BornName, secondary.BornName);
        primary.FatherName = Pick(taken, "Отчество", primary.FatherName, secondary.FatherName);
        primary.SurName = Pick(taken, "Фамилия", primary.SurName, secondary.SurName);
        primary.PrefferedName = Pick(taken, "Отображаемое имя", primary.PrefferedName, secondary.PrefferedName);

        MergeAuth(primary.Auth, secondary.Auth);

        if (secondary.Extra is not null)
        {
            // Настроек у основного нет вовсе — берём настройки поглощаемого, а не умолчания.
            primary.Extra ??= new UserExtra
            {
                UserId = primary.UserId,
                SocialNetworksAccess = secondary.Extra.SocialNetworksAccess,
                EnableTelegramPlayerDigestNotification = secondary.Extra.EnableTelegramPlayerDigestNotification,
            };
            var primaryHasVkLogin = primary.ExternalLogins.Any(l => l.Provider == UserExternalLogin.VkProvider);
            MergeExtra(taken, primary.Extra, secondary.Extra, primaryHasVkLogin);
        }

        if (secondary.Allrpg?.Sid is not null && primary.Allrpg?.Sid is null)
        {
            primary.Allrpg ??= new AllrpgUserDetails { UserId = primary.UserId };
            primary.Allrpg.Sid = secondary.Allrpg.Sid;
            primary.Allrpg.JsonProfile = secondary.Allrpg.JsonProfile;
            taken.Add("Профиль allrpg");
        }

        return taken;
    }

    private static void MergeAuth(UserAuthDetails primary, UserAuthDetails secondary)
    {
        // Пароль, email, подтверждение и админство — от основного; даты — по всей истории человека.
        if (secondary.RegisterDate < primary.RegisterDate)
        {
            primary.RegisterDate = secondary.RegisterDate;
        }
        if (secondary.LastLoginDate > primary.LastLoginDate || primary.LastLoginDate is null)
        {
            primary.LastLoginDate = secondary.LastLoginDate;
        }
        // Сбрасывает сессии и закешированные роли основного (SecurityStampValidator).
        primary.AspNetSecurityStamp = Guid.NewGuid().ToString();
    }

    private static void MergeExtra(List<string> taken, UserExtra primary, UserExtra secondary, bool primaryHasVkLogin)
    {
        if (primary.Gender == Gender.Unknown && secondary.Gender != Gender.Unknown)
        {
            primary.Gender = secondary.Gender;
            taken.Add("Пол");
        }
        primary.PhoneNumber = Pick(taken, "Телефон", primary.PhoneNumber, secondary.PhoneNumber)!;
        primary.Nicknames = Pick(taken, "Ники", primary.Nicknames, secondary.Nicknames)!;
        primary.GroupNames = Pick(taken, "Команды", primary.GroupNames, secondary.GroupNames)!;

        var takenBeforeContacts = taken.Count;
        if (string.IsNullOrWhiteSpace(primary.Vk) && !string.IsNullOrWhiteSpace(secondary.Vk))
        {
            primary.Vk = secondary.Vk;
            // Подтверждённым ВК поглощаемого считается, только если у основного нет своего ВК-логина:
            // при слиянии остаётся логин основного, и войти через ВК поглощаемого будет нельзя.
            primary.VkVerified = secondary.VkVerified && !primaryHasVkLogin;
            taken.Add("ВКонтакте");
        }
        primary.Livejournal = Pick(taken, "ЖЖ", primary.Livejournal, secondary.Livejournal);
        primary.Telegram = Pick(taken, "Telegram", primary.Telegram, secondary.Telegram);
        // Контакты, которые поглощаемый скрывал от игроков, не должны стать публичными.
        if (taken.Count > takenBeforeContacts && secondary.SocialNetworksAccess < primary.SocialNetworksAccess)
        {
            primary.SocialNetworksAccess = secondary.SocialNetworksAccess;
        }

        if (primary.BirthDate is null && secondary.BirthDate is not null)
        {
            primary.BirthDate = secondary.BirthDate;
            taken.Add("Дата рождения");
        }
        primary.PassportData = Pick(taken, "Паспортные данные", primary.PassportData, secondary.PassportData);
        primary.RegistrationAddress = Pick(taken, "Адрес регистрации", primary.RegistrationAddress, secondary.RegistrationAddress);
    }

    private static string? Pick(List<string> taken, string label, string? primary, string? secondary)
    {
        if (string.IsNullOrWhiteSpace(primary) && !string.IsNullOrWhiteSpace(secondary))
        {
            taken.Add(label);
            return secondary;
        }
        return primary;
    }
}
