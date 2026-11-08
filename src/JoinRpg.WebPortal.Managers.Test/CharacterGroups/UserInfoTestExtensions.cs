using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.WebPortal.Managers.Test.CharacterGroups;

/// <summary>
/// Строит <see cref="UserInfo"/> из EF-сущности мока. Неэффективно, если у пользователя
/// не загружены проекты, — в продакшен-коде UserInfo грузится из репозитория одним запросом.
/// </summary>
internal static class UserInfoTestExtensions
{
    public static UserInfo GetUserInfo(this User user)
    {
        var telegram = TelegramSocialLink.FromUserData(user.ExternalLogins.SingleOrDefault(x => x.Provider == UserExternalLogin.TelegramProvider)?.Key, PrefferedName.FromOptional(user.Extra?.Telegram));
        var vk = VkSocialLink.FromUserData(user.ExternalLogins.SingleOrDefault(x => x.Provider == UserExternalLogin.VkProvider)?.Key, user.Extra?.Vk, user.Extra?.VkVerified ?? false);

        return new UserInfo(
            user.GetId(),
            new UserSocialNetworks(telegram, LiveJournalId.FromOptional(user.Extra?.Livejournal), user.Allrpg?.Sid, vk, user.Extra?.SocialNetworksAccess ?? ContactsAccessType.Public),
            // Поле называется ActiveClaims — фильтр по активности тут был потерян.
            [.. user.Claims.Where(c => c.ClaimStatus.IsActive()).Select(c => new UserClaimInfo(c.GetId(), c.ClaimStatus))],
            user.ProjectAcls.Where(p => p.IsActive && p.Project.Active).Select(p => new ProjectIdentification(p.ProjectId)).ToList(),
            user.ProjectAcls.Where(p => p.IsActive).Select(p => new ProjectIdentification(p.ProjectId)).ToList(),
            user.Auth.IsAdmin,
            user.SelectedAvatar is { } avatar
                ? new UserAvatarSource(new AvatarIdentification(avatar.UserAvatarId), avatar.CachedUri, avatar.OriginalUri)
                : null,
            new Email(user.Email),
            user.Auth.EmailConfirmed,
            user.ExtractFullName(),
            user.VerifiedProfileFlag,
            user.Extra?.PhoneNumber,
            user.PasswordHash != null,
            user.Extra?.BirthDate is DateTime birthDate ? DateOnly.FromDateTime(birthDate) : null,
            user.Extra?.PassportData,
            user.Extra?.RegistrationAddress
            );
    }
}
