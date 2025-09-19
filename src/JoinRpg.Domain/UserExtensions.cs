using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Domain;

public static class UserExtensions
{
    public static UserProfileAccessReason GetProfileAccess(this User user, User? currentUser)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (currentUser == null)
        {
            return UserProfileAccessReason.NoAccess;
        }
        if (user.UserId == currentUser.UserId)
        {
            return UserProfileAccessReason.ItsMe;
        }
        if (user.Claims.Any(claim => claim.HasAccess(currentUser.UserId) && claim.ClaimStatus != Claim.Status.AddedByMaster))
        {
            return UserProfileAccessReason.Master;
        }
        if (user.ProjectAcls.Any(acl => acl.Project.HasMasterAccess(currentUser.UserId)))
        {
            return UserProfileAccessReason.CoMaster;
        }
        if (currentUser.Auth.IsAdmin == true)
        {
            return UserProfileAccessReason.Administrator;
        }
        return UserProfileAccessReason.NoAccess;
    }

    public static UserIdentification GetId(this User user) => new UserIdentification(user.UserId);

    public static UserInfo GetUserInfo(this User user)
    {
        var telegramId = TelegramId.FromOptional(user.ExternalLogins.SingleOrDefault(x => x.Provider == UserExternalLogin.TelegramProvider)?.Key, PrefferedName.FromOptional(user.Extra?.Telegram));

        var userFullName = user.ExtractFullName();
        return new UserInfo(
            user.GetId(),
            new UserSocialNetworks(telegramId, user.Extra?.Skype, user.Extra?.Livejournal, user.Allrpg.Sid, user.Extra?.Vk, user.Extra?.SocialNetworksAccess ?? ContactsAccessType.Public),
            user.Claims.Select(c => new ClaimIdentification(c.ProjectId, c.ClaimId)).ToList(),
            user.ProjectAcls.Where(p => p.Project.Active).Select(p => new ProjectIdentification(p.ProjectId)).ToList(),
            user.ProjectAcls.Select(p => new ProjectIdentification(p.ProjectId)).ToList(),
            user.Auth.IsAdmin,
            AvatarIdentification.FromOptional(user.SelectedAvatarId),
            new Email(user.Email),
            userFullName,
            user.VerifiedProfileFlag,
            user.Extra?.PhoneNumber
            );
    }

    public static UserInfo GetUserInfo(this Claim claim) => claim.Player.GetUserInfo();

    public static ContactsAccessType GetSocialNetworkAccess(this User user) => user.Extra?.SocialNetworksAccess ?? ContactsAccessType.OnlyForMasters;

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

    /// <summary>
    /// Returns display name of a user
    /// </summary>
    public static string GetDisplayName(this User user)
    {
        return user.ExtractDisplayName().DisplayName;
    }
}
