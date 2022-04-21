using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.Models.UserProfile;

namespace JoinRpg.Web.Models;

public class UserProfileViewModel
{
    public string DisplayName { get; set; }

    public IEnumerable<ProjectLinkViewModel> ThisUserProjects { get; set; }

    [ReadOnly(true)]
    public IEnumerable<ProjectLinkViewModel> CanGrantAccessProjects { get; set; } = Array.Empty<ProjectLinkViewModel>();
    public int UserId { get; set; }

    public IEnumerable<ProjectLinkViewModel> ProjectsToAdd
      => CanGrantAccessProjects.Where(acl => ThisUserProjects.All(acl1 => acl1.ProjectId != acl.ProjectId));

    [ReadOnly(true)]
    public ClaimListViewModel? Claims { get; set; }

    public UserProfileDetailsViewModel Details { get; set; }

    [ReadOnly(true)]
    public string Hash { get; set; }

    [ReadOnly(true)]
    public bool HasAdminAccess { get; set; }

    [ReadOnly(true)]
    public bool IsAdmin { get; set; }

    [ReadOnly(true)]
    public bool IsVerifiedUser => Details.IsVerifiedUser;
}

public class UserProfileDetailsViewModel
{
    [Display(Name = "Номер телефона"), DataType(DataType.PhoneNumber), UIHint("PhoneNumber")]
    public string PhoneNumber { get; }
    [Display(Name = "Skype"), UIHint("Skype")]
    public string? Skype { get; }
    [Display(Name = "Telegram"), UIHint("Telegram")]
    public string? Telegram { get; }
    [Display(Name = "ЖЖ"), UIHint("Livejournal")]
    public string? Livejournal { get; }
    [Display(Name = "VK"), UIHint("Vkontakte")]
    public string? Vk { get; }
    [UIHint("Email")]
    public string Email { get; }
    [DisplayName("ФИО")]
    public string FullName { get; }

    public int? AllrpgId { get; }

    public AvatarIdentification? Avatar { get; }
    [Editable(false)]
    public UserLinkViewModel User { get; }

    public AccessReason Reason { get; }

    public bool IsVerifiedUser { get; }
    public bool IsAdmin { get; }

    public ContactsAccessTypeView SocialNetworkAccess { get; }

    public UserProfileDetailsViewModel(User user, User currentUser)
        : this(user, (AccessReason)user.GetProfileAccess(currentUser))
    {

    }
    public UserProfileDetailsViewModel(User user, AccessReason reason)
    {
        User = new UserLinkViewModel(user);
        Reason = reason;
        SocialNetworkAccess = (ContactsAccessTypeView)user.GetSocialNetworkAccess();
        Avatar = AvatarIdentification.FromOptional(user.SelectedAvatarId);

        if (HasAccess)
        {
            Email = user.Email;
            FullName = user.FullName;
            Skype = user.Extra?.Skype;
            Telegram = user.Extra?.Telegram;
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
    [Display(Name = "Это мой профиль"), UsedImplicitly]
    ItsMe,
    [Display(Name = "Есть заявка на мою игру"), UsedImplicitly]
    Master,
    [Display(Name = "Со-мастер"), UsedImplicitly]
    CoMaster,
    [Display(Name = "Я вижу все, т.к. администратор сайта"), UsedImplicitly]
    Administrator,
}
