using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class AclViewModel
  {
    public int? ProjectAclId { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }

    [Display(Name="Мастер")]
    public User Master { get; set; }

    [DisplayName("Может принимать / отклонять заявки ")]
    public bool CanApproveClaims { get; set; }

    [DisplayName("Может настраивать поля персонажа")]
    public bool CanChangeFields { get; set; }

    [DisplayName("Может настраивать свойства базы заявок")]
    public bool CanChangeProjectProperties { get; set; }

    [DisplayName("Может давать доступ другим мастерам")]
    public bool CanGrantRights { get; set; }

    [Display(Name = "Игра")]
    public string ProjectName { get; set; }

    public static AclViewModel FromAcl(ProjectAcl acl)
    {
      return new AclViewModel()
      {
        ProjectId = acl.ProjectId,
        ProjectAclId = acl.ProjectAclId,
        UserId = acl.UserId,
        CanApproveClaims = acl.CanApproveClaims,
        CanChangeFields = acl.CanChangeFields,
        CanChangeProjectProperties = acl.CanChangeProjectProperties,
        CanGrantRights = acl.CanGrantRights,
        Master = acl.User, 
        ProjectName = acl.Project.ProjectName
      };
    }
  }

  public class CreateAclViewModel
  {
    public int ProjectId { get; set; }
    public int UserId { get; set; }

    [DisplayName("Может принимать / отклонять заявки ")]
    public bool CanApproveClaims
    { get; set; }

    [DisplayName("Может настраивать поля персонажа")]
    public bool CanChangeFields
    { get; set; }

    [DisplayName("Может настраивать свойства базы заявок")]
    public bool CanChangeProjectProperties
    { get; set; }

    [DisplayName("Может давать доступ другим мастерам")]
    public bool CanGrantRights
    { get; set; }
  }

}
