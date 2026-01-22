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

public enum MandatorySenstiveContactsView
{
    [Display(Name = "Не запрашивать", Description = "Игрока не будут просить предоставить доступ к этим данным. ")]
    Optional,

    [Display(Name = "Запросить",
        Description = "Попросить игрока предоставить доступ к этим данным. При незаполненном поле профиля заявка будет помечаться как проблемная.")]
    Request,
}

