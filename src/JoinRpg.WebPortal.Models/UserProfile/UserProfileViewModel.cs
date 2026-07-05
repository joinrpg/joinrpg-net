using System.ComponentModel;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models;

public class UserProfileViewModel
{
    public required string DisplayName { get; set; }

    public required IEnumerable<ProjectLinkViewModel> ThisUserProjects { get; set; }

    [ReadOnly(true)]
    public IEnumerable<ProjectLinkViewModel> CanGrantAccessProjects { get; set; } = [];
    public int UserId { get; set; }

    public IEnumerable<ProjectLinkViewModel> ProjectsToAdd => CanGrantAccessProjects.Except(ThisUserProjects);

    [ReadOnly(true)]
    public MyClaimListViewModel? Claims { get; set; }

    public required UserProfileDetailsViewModel Details { get; set; }

    [ReadOnly(true)]
    public bool HasAdminAccess => Admin is not null;

    [ReadOnly(true)]
    public bool IsAdmin { get; set; }

    [ReadOnly(true)]
    public bool IsVerifiedUser => Details.IsVerifiedUser;

    public required UserAdminOperationsViewModel? Admin { get; set; }
}

public record class UserAdminOperationsViewModel(Uri LogLink, bool IsAdmin);

public class UserProfileDetailsViewModel
{
    [Display(Name = "Номер телефона"), DataType(DataType.PhoneNumber)]
    public PhoneNumber? PhoneNumber { get; }
    [Display(Name = "Skype"), UIHint("Skype")]
    public string? Skype { get; }
    public TelegramId? Telegram { get; }
    [Display(Name = "ЖЖ")]
    public LiveJournalId? Livejournal { get; }
    [Display(Name = "VK")]
    public VkId? Vk { get; }
    public Email? Email { get; }
    public bool EmailConfirmed { get; }
    [DisplayName("ФИО")]
    public string? FullName { get; }

    public int? AllrpgId { get; }

    public AvatarIdentification? Avatar { get; }
    [Editable(false)]
    public UserLinkViewModel User { get; }

    public AccessReasonView Reason { get; }

    public bool IsVerifiedUser { get; }

    public bool ViewAsAdmin { get; }

    public ContactsAccessTypeView SocialNetworkAccess { get; }

    public bool IsMine => Reason == AccessReasonView.ItsMe;

    public UserProfileDetailsViewModel(UserInfo user, UserInfo? currentUser)
        : this(user, user.GetAccess(currentUser), currentUser?.IsAdmin ?? false) { }

    public UserProfileDetailsViewModel(UserInfo user, ProjectInfo currentProject, ICurrentUserAccessor currentUserAccessor)
    : this(user, user.GetAccess(currentProject), currentUserAccessor.IsAdmin) { }

    [Obsolete("Передайте сюда ICurrentUserAccessor или UserInfo")]
    public UserProfileDetailsViewModel(UserInfo user, ProjectInfo currentProject)
    : this(user, user.GetAccess(currentProject), false) { }

    public UserProfileDetailsViewModel(UserInfoHeader user)
    {
        User = UserLinks.Create(user);
        Reason = AccessReasonView.CoMaster;
    }

    private UserProfileDetailsViewModel(UserInfo user, UserProfileAccessReason accessReason, bool viewAsAdmin)
    {
        User = UserLinks.Create(user, ViewMode.Show);
        Reason = (AccessReasonView)accessReason;
        SocialNetworkAccess = (ContactsAccessTypeView)user.Social.SocialNetworksAccess;
        Avatar = user.SelectedAvatarId;

        if (Reason != AccessReasonView.NoAccess)
        {
            Email = user.Email;
            EmailConfirmed = user.EmailConfirmed;
            FullName = user.UserFullName.FullName;
            PhoneNumber = PhoneNumber.FromOptional(user.PhoneNumber);
            IsVerifiedUser = user.VerifiedProfileFlag;
        }
        if (Reason != AccessReasonView.NoAccess || user.Social.SocialNetworksAccess == ContactsAccessType.Public)
        {
            Vk = VkId.FromOptional(user.Social.VkId);
            AllrpgId = user.Social.AllrpgInfoId;
            Telegram = user.Social.TelegramId;
            Livejournal = LiveJournalId.FromOptional(user.Social.LiveJournal);
            HasSocialAccess = true;
        }

        ViewAsAdmin = viewAsAdmin;
    }

    public bool HasSocialAccess { get; }
}

public enum AccessReasonView
{
    NoAccess,
    [Display(Name = "Это мой профиль")]
    ItsMe,
    [Display(Name = "Есть заявка на мою игру")]
    Master,
    [Display(Name = "Со-мастер")]
    CoMaster,
    [Display(Name = "Я вижу все, т.к. администратор сайта")]
    Administrator,
}
