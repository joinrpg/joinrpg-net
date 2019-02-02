using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class UserProfileViewModel
  {
    public string DisplayName { get; set; }

    public IEnumerable<ProjectAcl> ThisUserProjects { get; set; }

    [ReadOnly(true)]
    public IEnumerable<Project> CanGrantAccessProjects { get; set; } = new Project[] {};
    public int UserId { get; set; }

    public IEnumerable<Project> ProjectsToAdd
      => CanGrantAccessProjects.Where(acl => ThisUserProjects.All(acl1 => acl1.ProjectId != acl.ProjectId));

    [ReadOnly(true)]
    public ClaimListViewModel Claims { get; set; }

    public UserProfileDetailsViewModel Details { get; set; }

    [ReadOnly(true)]
    public string Hash { get; set; }

    [ReadOnly(true)]
    public bool HasAdminAccess { get; set; }

    [ReadOnly(true)]
    public bool IsAdmin { get; set;  }
  }

  public class UserProfileDetailsViewModel
  {
    [Display(Name = "Номер телефона"), DataType(DataType.PhoneNumber), UIHint("PhoneNumber")]
    public string PhoneNumber { get; }
    [Display(Name = "Skype"), UIHint("Skype")]
    public string Skype { get; }
    [Display(Name = "Telegram"), UIHint("Telegram")]
    public string Telegram { get; }
    [Display(Name = "ЖЖ"), UIHint("Livejournal")]
    public string Livejournal { get; }
    [Display(Name = "VK"), UIHint("Vkontakte")]
    public string Vk { get;  }
    [UIHint("Email")]
    public string Email { get;  }
    [DisplayName("ФИО")]
    public string FullName { get; }

    public int? AllrpgId { get; }

    public User User { get; } //TODO: Start using ViewModel here

    public AccessReason Reason { get; }

    public UserProfileDetailsViewModel ([NotNull] User user, AccessReason reason)
    {
      if (user == null) throw new ArgumentNullException(nameof(user));
      Email = user.Email;
      FullName = user.FullName;
      User = user;
      Reason = reason;
      Skype = user.Extra?.Skype;
      Telegram = user.Extra?.Telegram;
      Livejournal = user.Extra?.Livejournal;
      Vk = user.Extra?.Vk;
      PhoneNumber = user.Extra?.PhoneNumber ?? "";
      AllrpgId = user.Allrpg?.Sid;
    }

    public bool HasAccess => Reason != AccessReason.NoAccess;
  }

  public enum AccessReason
  {
    NoAccess,
    [Display(Name="Это мой профиль"),UsedImplicitly]
    ItsMe,
    [Display(Name = "Есть заявка на мою игру"),UsedImplicitly]
    Master,
    [Display(Name = "Со-мастер"),UsedImplicitly]
    CoMaster,
    [Display(Name = "Я вижу все, т.к. администратор сайта"),UsedImplicitly]
    Administrator,
  }
}
