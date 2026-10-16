using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Web.UserProfile;

/// <summary>
/// Карточка пользователя (<see cref="UserProfilePanel"/>): аватарка, имя и те контакты, которые смотрящему можно видеть.
/// </summary>
public class UserProfileDetailsViewModel
{
    public PhoneNumber? PhoneNumber { get; }
    public TelegramSocialLink? Telegram { get; }
    public LiveJournalId? Livejournal { get; }
    public VkSocialLink? Vk { get; }
    public Email? Email { get; }
    public bool EmailConfirmed { get; }
    public bool HasPassword { get; }
    public string? FullName { get; }

    public int? AllrpgId { get; }

    public AvatarIdentification? Avatar { get; }

    public UserLinkViewModel User { get; }

    public AccessReasonView Reason { get; }

    public bool IsVerifiedUser { get; }

    /// <summary>
    /// «Совершеннолетний» или точный возраст — саму дату рождения эта ViewModel никогда не несёт.
    /// </summary>
    public string? AgeMarker { get; }

    public bool ViewAsAdmin { get; }

    public bool IsMine => Reason == AccessReasonView.ItsMe;

    public UserProfileDetailsViewModel(UserInfo user, UserInfo? currentUser)
        : this(user, user.GetAccess(currentUser), currentUser?.IsAdmin ?? false) { }

    public UserProfileDetailsViewModel(UserInfo user, ProjectInfo currentProject, bool viewAsAdmin)
    : this(user, user.GetAccess(currentProject), viewAsAdmin) { }

    /// <summary>
    /// Карточка мастера проекта: для текущего пользователя — его собственный профиль, для остальных — со-мастер.
    /// </summary>
    public UserProfileDetailsViewModel(UserInfoHeader user, UserIdentification? currentUserId)
    {
        User = new UserLinkViewModel(user);
        Reason = user.UserId == currentUserId ? AccessReasonView.ItsMe : AccessReasonView.CoMaster;
    }

    private UserProfileDetailsViewModel(UserInfo user, UserProfileAccessReason accessReason, bool viewAsAdmin)
    {
        User = new UserLinkViewModel(user.ToUserInfoHeader());
        Reason = (AccessReasonView)accessReason;
        Avatar = user.SelectedAvatarId;

        if (Reason != AccessReasonView.NoAccess)
        {
            Email = user.Email;
            EmailConfirmed = user.EmailConfirmed;
            HasPassword = user.HasPassword;
            FullName = user.UserFullName.FullName;
            PhoneNumber = PhoneNumber.FromOptional(user.PhoneNumber);
            IsVerifiedUser = user.VerifiedProfileFlag;
            AgeMarker = FormatAgeMarker(user, DateOnly.FromDateTime(DateTime.UtcNow));
        }
        if (Reason != AccessReasonView.NoAccess || user.Social.SocialNetworksAccess == ContactsAccessType.Public)
        {
            Vk = user.Social.Vk;
            AllrpgId = user.Social.AllrpgInfoId;
            Telegram = user.Social.Telegram;
            Livejournal = user.Social.LiveJournal;
            HasSocialAccess = true;
        }

        ViewAsAdmin = viewAsAdmin;
    }

    public bool HasSocialAccess { get; }

    /// <summary>
    /// «Совершеннолетний» или точный возраст — то же самое, что видят мастера в профиле игрока.
    /// Используется и здесь, и на странице настроек профиля, чтобы пользователь видел ту же пометку.
    /// </summary>
    public static string? FormatAgeMarker(UserInfo user, DateOnly today)
    {
        if (user.GetAgeOn(today) is not int age)
        {
            return null;
        }
        return age >= 18 ? "Совершеннолетний" : CountHelper.DisplayCount(age, "год", "года", "лет");
    }
}

public enum AccessReasonView
{
    NoAccess,
    ItsMe,
    Master,
    CoMaster,
    Administrator,
}
