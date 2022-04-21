using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;
using JoinRpg.DataModel.Users;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by LINQ
public class User
{
    public int UserId { get; set; }

    public override string ToString() => $"User(UserId: {UserId}, BornName: {BornName}, FatherName: {FatherName}, SurName: {SurName}, Id: {UserId}, UserName: {UserName}, Email: {Email}, PasswordHash: {PasswordHash}, ProjectAcls: {ProjectAcls.Select(acl => acl.Project.ProjectName).JoinStrings(", ")}, Claims: {Claims}, FullName: {FullName}, PrefferedName: {PrefferedName}, Auth: {Auth}, Allrpg: {Allrpg}, Extra: {Extra}, Subscriptions: {Subscriptions})";

    public string? BornName { get; set; }

    public string? FatherName { get; set; }

    public string? SurName { get; set; }

    public string UserName { get; set; }
    public string Email { get; set; }

    public string PasswordHash { get; set; }

    public virtual ICollection<ProjectAcl> ProjectAcls { get; set; } = new HashSet<ProjectAcl>();

    public virtual ICollection<Claim> Claims { get; set; }

    public string FullName => new[] { BornName, FatherName, SurName }.JoinIfNotNullOrWhitespace(" ");

    public string PrefferedName { get; set; }

    [DisplayName("Подтвержденный профиль")]
    public bool VerifiedProfileFlag { get; set; }

    [NotNull]
    public virtual UserAuthDetails Auth { get; set; }

    public virtual AllrpgUserDetails Allrpg { get; set; }
    [CanBeNull]
    public virtual UserExtra? Extra { get; set; }

    public virtual HashSet<UserSubscription> Subscriptions { get; set; }

    public virtual ICollection<UserExternalLogin> ExternalLogins { get; set; } = new List<UserExternalLogin>();

    public const string OnlinePaymentVirtualUser = "payments@joinrpg.ru";

    public virtual UserAvatar SelectedAvatar { get; set; }

    [ForeignKey(nameof(SelectedAvatar))]
    public int? SelectedAvatarId { get; set; }

    public ICollection<UserAvatar> Avatars { get; set; } = new HashSet<UserAvatar>();
}
