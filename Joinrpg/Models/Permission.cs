using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
  public enum Permission
  {
    None,
    [Display(Name = "Настраивать поля персонажа")]
    CanChangeFields,
    [Display(Name = "Настраивать проект")]
    CanChangeProjectProperties,
    [Display(Name = "Давать доступ другим мастерам")]
    CanGrantRights,
    [Display(Name = "Администратор заявок")]
    CanManageClaims,
    [Display(Name = "Редактировать ролевку")]
    CanEditRoles,
    [Display(Name = "Управлять финансами")]
    CanManageMoney,
    [Display(Name = "Делать массовые рассылки")]
    CanSendMassMails,
    [Display(Name = "Редактор сюжетов")]
    CanManagePlots,
  }
}