using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes;
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

    public IEnumerable<ProjectLinkViewModel> ProjectsToAdd
      => CanGrantAccessProjects.Where(acl => ThisUserProjects.All(acl1 => acl1.ProjectId != acl.ProjectId));

    [ReadOnly(true)]
    public ClaimListViewModel? Claims { get; set; }

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
    [DisplayName("ФИО")]
    public string? FullName { get; }

    public int? AllrpgId { get; }

    public AvatarIdentification? Avatar { get; }
    [Editable(false)]
    public UserLinkViewModel User { get; }

    public AccessReason Reason { get; }

    public bool IsVerifiedUser { get; }
    public bool IsAdmin { get; }

    public ContactsAccessTypeView SocialNetworkAccess { get; }

    public UserProfileDetailsViewModel(User user, User? currentUser)
        : this(user, (AccessReason)user.GetProfileAccess(currentUser))
    {

    }
    public UserProfileDetailsViewModel(User user, AccessReason reason)
    {
        User = UserLinks.Create(user, ViewMode.Show);
        Reason = reason;
        SocialNetworkAccess = (ContactsAccessTypeView)user.GetSocialNetworkAccess();
        Avatar = AvatarIdentification.FromOptional(user.SelectedAvatarId);

        if (HasAccess)
        {
            Email = user.Email;
            FullName = user.FullName;
            Skype = user.Extra?.Skype;
            Telegram = user.Extra?.Telegram?.TrimStart('@');
            Livejournal = user.Extra?.Livejournal;
            PhoneNumber = user.Extra?.PhoneNumber ?? "";
            IsVerifiedUser = user.VerifiedProfileFlag;
            IsAdmin = user.Auth.IsAdmin;
        }
        if (HasAccess || user.Extra?.SocialNetworksAccess == ContactsAccessType.Public)
        {
            Vk = user.Extra?.Vk;
            AllrpgId = user.Allrpg?.Sid;
        }
    }

    public bool HasAccess => Reason != AccessReason.NoAccess;

}

public enum AccessReason
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
