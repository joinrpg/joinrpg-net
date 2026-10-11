using JoinRpg.DataModel;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Слияние профиля при слиянии пользователей (ADR024): значение основного побеждает, пустые поля
/// основного заполняются из поглощаемого. Работает на сущностях, сохраняет вызывающий.
/// </summary>
public static class UserProfileMerger
{
    public static void MergeInto(User primary, User secondary)
    {
        primary.BornName = Pick(primary.BornName, secondary.BornName);
        primary.FatherName = Pick(primary.FatherName, secondary.FatherName);
        primary.SurName = Pick(primary.SurName, secondary.SurName);
        primary.PrefferedName = Pick(primary.PrefferedName, secondary.PrefferedName);

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
            MergeExtra(primary.Extra, secondary.Extra);
        }

        if (secondary.Allrpg?.Sid is not null && primary.Allrpg?.Sid is null)
        {
            primary.Allrpg ??= new AllrpgUserDetails { UserId = primary.UserId };
            primary.Allrpg.Sid = secondary.Allrpg.Sid;
            primary.Allrpg.JsonProfile = secondary.Allrpg.JsonProfile;
        }
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

    private static void MergeExtra(UserExtra primary, UserExtra secondary)
    {
        if (primary.Gender == Gender.Unknown)
        {
            primary.Gender = secondary.Gender;
        }
        primary.PhoneNumber = Pick(primary.PhoneNumber, secondary.PhoneNumber)!;
        primary.Nicknames = Pick(primary.Nicknames, secondary.Nicknames)!;
        primary.GroupNames = Pick(primary.GroupNames, secondary.GroupNames)!;

        var contactsTaken = false;
        if (string.IsNullOrWhiteSpace(primary.Vk) && !string.IsNullOrWhiteSpace(secondary.Vk))
        {
            primary.Vk = secondary.Vk;
            primary.VkVerified = secondary.VkVerified;
            contactsTaken = true;
        }
        if (string.IsNullOrWhiteSpace(primary.Livejournal) && !string.IsNullOrWhiteSpace(secondary.Livejournal))
        {
            primary.Livejournal = secondary.Livejournal;
            contactsTaken = true;
        }
        if (string.IsNullOrWhiteSpace(primary.Telegram) && !string.IsNullOrWhiteSpace(secondary.Telegram))
        {
            primary.Telegram = secondary.Telegram;
            contactsTaken = true;
        }
        // Контакты, которые поглощаемый скрывал от игроков, не должны стать публичными.
        if (contactsTaken && secondary.SocialNetworksAccess < primary.SocialNetworksAccess)
        {
            primary.SocialNetworksAccess = secondary.SocialNetworksAccess;
        }

        primary.BirthDate ??= secondary.BirthDate;
        primary.PassportData = Pick(primary.PassportData, secondary.PassportData);
        primary.RegistrationAddress = Pick(primary.RegistrationAddress, secondary.RegistrationAddress);
    }

    private static string? Pick(string? primary, string? secondary)
        => string.IsNullOrWhiteSpace(primary) ? secondary : primary;
}
