using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by LINQ
    public class User
    {
        public int UserId { get; set; }

        public override string ToString() => $"User(UserId: {UserId}, BornName: {BornName}, FatherName: {FatherName}, SurName: {SurName}, Id: {UserId}, UserName: {UserName}, Email: {Email}, PasswordHash: {PasswordHash}, ProjectAcls: {ProjectAcls.Select(acl => acl.Project.ProjectName).JoinStrings(", ")}, Claims: {Claims}, FullName: {FullName}, PrefferedName: {PrefferedName}, Auth: {Auth}, Allrpg: {Allrpg}, Extra: {Extra}, Subscriptions: {Subscriptions})";

        public string BornName { get; set; }

        public string FatherName { get; set; }

        public string SurName { get; set; }

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
        public virtual UserExtra Extra { get; set; }

        public virtual HashSet<UserSubscription> Subscriptions { get; set; }

        public virtual ICollection<UserExternalLogin> ExternalLogins { get; set; } = new List<UserExternalLogin>();

        public const string OnlinePaymentVirtualUser = "payments@joinrpg.ru";

    }

    public enum Gender : byte
    {
        Unknown = 0,
        Male = 1,
        Female = 2,
    }

    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class UserExternalLogin
    {
        public int UserExternalLoginId { get; set; }
        public int UserId { get; set; }
        [NotNull]
        public virtual User User { get; set; }
        public string Provider { get; set; }
        public string Key { get; set; }
    }

    public class UserExtra
    {
        public int UserId { get; set; }
        public byte GenderByte { get; set; }

        public Gender Gender
        {
            get => (Gender)GenderByte;
            set => GenderByte = (byte)value;
        }

        public string PhoneNumber { get; set; }
        public string Skype { get; set; }

        public string Vk { get; set; }
        public string Livejournal { get; set; }

        public string Nicknames { get; set; }

        public string GroupNames { get; set; }

        public string Telegram { get; set; }

        public DateTime? BirthDate { get; set; }

        public override string ToString() => $"UserExtra(UserId: {UserId}, Gender: {Gender}, PhoneNumber: {PhoneNumber}, Nicknames: {Nicknames}, GroupNames: {GroupNames}, BirthDate: {BirthDate}, Telegram: {Telegram})";
    }

    public class AllrpgUserDetails
    {
        public int UserId { get; set; }
        public int? Sid { get; set; }
        public string JsonProfile { get; set; }

        [Obsolete("Not used anymore")]
        public bool PreventAllrpgPassword { get; set; }

        public override string ToString() => $"AllrpgUser(UserId: {UserId}, Sid: {Sid}, JsonProfile: {JsonProfile}";
    }
}
