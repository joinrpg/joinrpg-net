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
    public AccessReason Reason { get; set; }

    [ReadOnly(true)]
    public string Hash { get; set; }

    [ReadOnly(true)]
    public bool HasAdminAccess { get; set; }

    [ReadOnly(true)]
    public bool IsAdmin { get; set;  }
  }

  public class UserProfileDetailsViewModel
  {
    [Display(Name = "Номер телефона"), DataType(DataType.PhoneNumber)]
    public string PhoneNumber { get; set; }
    [Display(Name = "Skype"), UIHint("Skype")]
    public string Skype { get; set; }
    [Display(Name = "ЖЖ"), UIHint("Livejournal")]
    public string Livejournal { get; set; }
    [Display(Name = "VK"), UIHint("Vkontakte")]
    public string Vk { get; set; }
    [UIHint("Email")]
    public string Email { get; set; }
    [DisplayName("ФИО")]
    public string FullName { get; set; }

    public int? AllrpgId { get; set; }

    public User User { get; set; } //TODO: Start using ViewModel here

    [CanBeNull]
    public static UserProfileDetailsViewModel FromUser([CanBeNull] User user)
    {
      if (user == null)
      {
        return null;
      }
      return new UserProfileDetailsViewModel()
      {
        Email = user.Email,
        FullName = user.FullName,
        User = user,
        Skype = user.Extra?.Skype,
        Livejournal = user.Extra?.Livejournal,
        Vk = user.Extra?.Vk,
        PhoneNumber = user.Extra?.PhoneNumber ?? "",
        AllrpgId = user.Allrpg?.Sid,
      };
    }
  }

  public enum AccessReason
  {
    NoAccess,
    [Display(Name="Это мой профиль")]
    ItsMe,
    [Display(Name = "Есть заявка на мою игру")]
    Master,
    [Display(Name = "Со-мастер")]
    CoMaster,
    [Display(Name = "Я вижу все, т.к. администратор сайта")]
    Administrator
  }
}
