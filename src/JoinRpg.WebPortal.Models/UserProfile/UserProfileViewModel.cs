using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.PrimitiveTypes.Users;
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
    [Display(Name = "Номер телефона"), DataType(DataType.PhoneNumber), UIHint("PhoneNumber")]
    public string? PhoneNumber { get; }
    [Display(Name = "Skype"), UIHint("Skype")]
    public string? Skype { get; }
    [Display(Name = "Telegram"), UIHint("Telegram")]
    public string? Telegram { get; }
    [Display(Name = "ЖЖ"), UIHint("Livejournal")]
    public string? Livejournal { get; }
    [Display(Name = "VK"), UIHint("Vkontakte")]
    public string? Vk { get; }
    [UIHint("Email")]
    public string? Email { get; }
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
            PhoneNumber = user.PhoneNumber;
            IsVerifiedUser = user.VerifiedProfileFlag;
        }
        if (Reason != AccessReasonView.NoAccess || user.Social.SocialNetworksAccess == ContactsAccessType.Public)
        {
            Vk = user.Social.VkId;
            AllrpgId = user.Social.AllrpgInfoId;
            Telegram = user.Social.TelegramId?.UserName?.Value;
            Livejournal = user.Social.LiveJournal;
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
