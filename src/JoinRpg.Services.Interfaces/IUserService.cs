namespace JoinRpg.Services.Interfaces;

public interface IUserService
{
    Task UpdateProfile(int userId, UserFullName userFullName, Gender gender, string phoneNumber, string nicknames, string groupNames, string livejournal, ContactsAccessType socialNetworkAccess, string passportData, string registrationAddress, DateOnly? birthDate);
    Task SetAdminFlag(int userId, bool administratorFlag);
    Task SetVerificationFlag(int userId, bool verificationFlag);
    /// <summary>
    /// Set user name data if not set already.
    /// All access check fortfeit (cause is method typically called during login, so ICurrentUserAccessor could be old).
    /// NOP for verified users
    /// </summary>
    Task SetNameIfNotSetWithoutAccessChecks(int userId, UserFullName userFullName);

    /// <summary>
    /// Set vk Link if not set already.
    /// All access check fortfeit (cause is method typically called during login, so ICurrentUserAccessor could be old).
    /// </summary>
    Task SetVkIfNotSetWithoutAccessChecks(int id, VkSocialLink vk, AvatarInfo? avatarInfo);

    /// <summary>
    /// Set birth date if not set already (e.g. pulled from VK on login). Never overwrites an already-set value.
    /// </summary>
    Task SetBirthDateIfNotSetWithoutAccessChecks(UserIdentification userId, DateOnly birthDate);

    /// <summary>
    /// Admin-only: set or clear birth date, bypassing the "already set" lock.
    /// </summary>
    Task SetBirthDate(UserIdentification userId, DateOnly? birthDate);


    Task SetTelegramIfNotSetWithoutAccessChecks(int id, TelegramSocialLink telegram, AvatarInfo? avatarInfo);

    Task RemoveVkFromProfile(UserIdentification id);
    Task RemoveTelegramFromProfile(int id);
}
