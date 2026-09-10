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
    bool HasPassword,
    DateOnly? BirthDate = null)
{
    public UserDisplayName DisplayName { get; } = new UserDisplayName(UserFullName, Email);

    // Не реализовано
    public bool PhoneNumberConfirmed { get; } = false;

    /// <summary>
    /// Телефон заполнен и не является заведомо неполным (короче <see cref="MinContactLength"/> символов).
    /// </summary>
    public bool HasCorrectPhone => IsCorrectContact(PhoneNumber);

    /// <summary>
    /// ФИО заполнено и не является заведомо неполным (короче <see cref="MinContactLength"/> символов).
    /// </summary>
    public bool HasCorrectRealName => IsCorrectContact(UserFullName.FullName);

    /// <summary>
    /// Минимальная длина значения телефона/ФИО, при которой оно считается заполненным.
    /// </summary>
    public const int MinContactLength = 5;

    public static bool IsCorrectContact(string? value) => (value?.Length ?? 0) >= MinContactLength;

    /// <summary>
    /// Число полных лет на дату <paramref name="today"/>, или null, если дата рождения не указана.
    /// </summary>
    /// <remarks>
    /// В день самого рождения человеку ещё не хватает года: по российской правовой традиции
    /// возраст считается наступившим с 00:00 суток, следующих за днём рождения (см. п. 7
    /// Постановления Пленума Верховного Суда РФ от 01.02.2011 № 1 — в отношении 14/16/18 лет,
    /// но правило общеупотребимо и для иного возраста). Поэтому дата годовщины считается ещё
    /// не наступившей, если она совпадает с <paramref name="today"/>, а не только если она позже.
    /// </remarks>
    public int? GetAgeOn(DateOnly today)
    {
        if (BirthDate is not DateOnly birthDate)
        {
            return null;
        }
        var age = today.Year - birthDate.Year;
        if (birthDate.AddYears(age) >= today)
        {
            age--;
        }
        return age;
    }

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
