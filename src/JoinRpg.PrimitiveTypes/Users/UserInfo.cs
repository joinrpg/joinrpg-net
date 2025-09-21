using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.PrimitiveTypes.Users;
public record class UserInfo(
    UserIdentification UserId,
    UserSocialNetworks Social,
    IReadOnlyCollection<ClaimIdentification> ActiveClaims, // Для заявок хорошо бы иметь статусы
    IReadOnlyCollection<ProjectIdentification> ActiveProjects,
    IReadOnlyCollection<ProjectIdentification> AllProjects,
    bool IsAdmin,
    AvatarIdentification? SelectedAvatarId,
    Email Email,
    UserFullName UserFullName,
    bool VerifiedProfileFlag,
    string? PhoneNumber)
{
    public UserNotificationSettings NotificationSettings { get; } = new UserNotificationSettings(true);//TODO из базы грузить

    public UserDisplayName DisplayName { get; } = new UserDisplayName(UserFullName, Email);

    public UserProfileAccessReason GetAccess(UserInfo? currentUser)
    {
        if (currentUser == null)
        {
            return UserProfileAccessReason.NoAccess;
        }
        if (currentUser.UserId == UserId)
        {
            return UserProfileAccessReason.ItsMe;
        }
        if (ActiveClaims.Select(x => x.ProjectId).Intersect(currentUser.ActiveProjects).Any())
        {
            return UserProfileAccessReason.Master;
        }
        if (ActiveProjects.Intersect(currentUser.ActiveProjects).Any())
        {
            return UserProfileAccessReason.CoMaster;
        }
        if (currentUser.IsAdmin)
        {
            return UserProfileAccessReason.Administrator;
        }
        return UserProfileAccessReason.NoAccess;
    }

    public UserProfileAccessReason GetAccess(ProjectInfo currentProject)
    {
        if (ActiveClaims.Any(x => x.ProjectId == currentProject.ProjectId))
        {
            return UserProfileAccessReason.Master;
        }
        if (ActiveProjects.Any(x => x == currentProject.ProjectId))
        {
            return UserProfileAccessReason.CoMaster;
        }
        return UserProfileAccessReason.NoAccess;
    }
}

public record class UserSocialNetworks(
    TelegramId? TelegramId,
    string? LiveJournal,
    int? AllrpgInfoId,
    string? VkId,
    ContactsAccessType SocialNetworksAccess);
public record class UserNotificationSettings(bool TelegramDigestEnabled);
