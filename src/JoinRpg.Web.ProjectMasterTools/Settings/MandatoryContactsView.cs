using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.Settings;
public enum MandatoryContactsView
{
    [Display(Name = "Все равно")]
    Optional,

    [Display(Name = "Рекомендованное",
        Description = "При незаполненном поле профиля заявка будет помечаться как проблемная.")]
    Recommended,

    [Display(Name = "Обязательное",
        Description = "Подать заявку при незаполненном поле профиля будет невозможно.")]
    Required,
}

