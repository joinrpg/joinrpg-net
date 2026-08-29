using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Helpers;

namespace JoinRpg.DomainTypes.Users;

public record class UserInfo(
    UserIdentification UserId,
    UserSocialNetworks Social,
    IReadOnlyCollection<ClaimIdentification> ActiveClaims, // Для заявок хорошо бы иметь статусы
    IReadOnlyCollection<ProjectIdentification> ActiveProjects,
    IReadOnlyCollection<ProjectIdentification> AllProjects,
    bool IsAdmin,
    AvatarIdentification? SelectedAvatarId,
    Email Email,
    bool EmailConfirmed,
    UserFullName UserFullName,
    bool VerifiedProfileFlag,
    string? PhoneNumber,
    bool HasPassword)
{
    public UserDisplayName DisplayName { get; } = new UserDisplayName(UserFullName, Email);

    // Не реализовано
    public bool PhoneNumberConfirmed { get; } = false;

    /// <summary>
    /// Есть ровно один способ войти в аккаунт. Привязка Telegram в расчёт не идёт — виджет
    /// умеет только привязывать контакт к уже залогиненному аккаунту, отдельного входа
    /// через него нет (см. <see cref="SocialLink.CanLogin"/>).
    /// </summary>
    public bool HasSingleLoginMethod =>
        (HasPassword ? 1 : 0)
        + Social.SocialLinks.Count(x => x.CanLogin)
        == 1;

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
    TelegramSocialLink? Telegram,
    string? LiveJournal,
    int? AllrpgInfoId,
    VkSocialLink? Vk,
    ContactsAccessType SocialNetworksAccess)
{
    public IReadOnlyCollection<SocialLink> SocialLinks { get; } = [.. new SocialLink?[] { Telegram, Vk }.WhereNotNull()];
}
