using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Web.Helpers;

namespace JoinRpg.Web.Models
{
  public class AclViewModel
  {
    public int? ProjectAclId { get; set; }
    [Display(Name="Проект")]
    public int ProjectId { get; set; }
    public int UserId { get; set; }

    [Display(Name = "Администратор заявок")]
    public bool CanManageClaims { get; set; }

    [Display(Name = "Настраивать поля персонажа")]
    public bool CanChangeFields { get; set; }

    [Display(Name="Настраивать проект")]
    public bool CanChangeProjectProperties { get; set; }

    [Display(Name = "Давать доступ другим мастерам")]
    public bool CanGrantRights { get; set; }

    [Display(Name="Редактировать ролевку")]
    public bool CanEditRoles { get; set; }

    [Display(Name = "Управлять финансами")]
    public bool CanManageMoney { get; set; }

    [Display(Name = "Игра"), ReadOnly(true)]
    public string ProjectName { get; set; }

    [Display(Name = "Заявок"), ReadOnly(true)]
    public int ClaimsCount { get; set; }

    [Display(Name = "Делать массовые рассылки")]
    public bool CanSendMassMails { get; set; }

    [Display(Name = "Редактор сюжетов")]
    public bool CanManagePlots { get; set; }

    [ReadOnly(true), Display(Name = "Мастер")]
    public UserProfileDetailsViewModel UserDetails { get; set; }

    [ReadOnly(true)]
    public IEnumerable<GameObjectLinkViewModel> ResponsibleFor { get; set; }

    public static AclViewModel FromAcl(ProjectAcl acl, int count, IReadOnlyCollection<CharacterGroup> groups)
    {
      return new AclViewModel()
      {
        ProjectId = acl.ProjectId,
        ProjectAclId = acl.ProjectAclId,
        UserId = acl.UserId,
        CanManageClaims = acl.CanManageClaims,
        CanChangeFields = acl.CanChangeFields,
        CanChangeProjectProperties = acl.CanChangeProjectProperties,
        CanGrantRights = acl.CanGrantRights,
        CanEditRoles = acl.CanEditRoles,
        CanManageMoney = acl.CanManageMoney,
        CanSendMassMails = acl.CanSendMassMails,
        CanManagePlots = acl.CanManagePlots,
        ProjectName = acl.Project.ProjectName,
        ClaimsCount = count,
        UserDetails = new UserProfileDetailsViewModel(acl.User),
        ResponsibleFor = groups.Select(group => group.AsObjectLink()),
      };
    }
  }
}
