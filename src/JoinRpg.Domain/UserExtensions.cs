using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Domain;

public static class UserExtensions
{
    public static UserIdentification GetId(this User user) => new UserIdentification(user.UserId);

    public static UserInfo GetUserInfo(this User user)
    {
        var telegramId = TelegramId.FromOptional(user.ExternalLogins.SingleOrDefault(x => x.Provider == UserExternalLogin.TelegramProvider)?.Key, PrefferedName.FromOptional(user.Extra?.Telegram));

        return new UserInfo(
            user.GetId(),
            new UserSocialNetworks(telegramId, user.Extra?.Livejournal, user.Allrpg?.Sid, user.Extra?.Vk, user.Extra?.SocialNetworksAccess ?? ContactsAccessType.Public),
            user.Claims.Select(c => c.GetId()).ToList(),
            user.ProjectAcls.Where(p => p.Project.Active).Select(p => new ProjectIdentification(p.ProjectId)).ToList(),
            user.ProjectAcls.Select(p => new ProjectIdentification(p.ProjectId)).ToList(),
            user.Auth.IsAdmin,
            AvatarIdentification.FromOptional(user.SelectedAvatarId),
            new Email(user.Email),
            user.Auth.EmailConfirmed,
            user.ExtractFullName(),
            user.VerifiedProfileFlag,
            user.Extra?.PhoneNumber
            );
    }

    public static UserInfo GetUserInfo(this Claim claim) => claim.Player.GetUserInfo();

    public static UserFullName ExtractFullName(this User user)
    {
        return new UserFullName(
            PrefferedName.FromOptional(user.PrefferedName),
            BornName.FromOptional(user.BornName),
            SurName.FromOptional(user.SurName),
            FatherName.FromOptional(user.FatherName));
    }

    public static UserDisplayName ExtractDisplayName(this User user)
    {
        return new UserDisplayName(user.ExtractFullName(), new Email(user.Email));
    }

    public static UserInfoHeader ToUserInfoHeader(this User user) => new(new UserIdentification(user.UserId), user.ExtractDisplayName());

    /// <summary>
    /// Returns display name of a user
    /// </summary>
    public static string GetDisplayName(this User user)
    {
        return user.ExtractDisplayName().DisplayName;
    }
}
