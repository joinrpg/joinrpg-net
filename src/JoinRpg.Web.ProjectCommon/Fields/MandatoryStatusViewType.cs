using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectCommon.Fields;
public enum MandatoryStatusViewType
{
    [Display(Name = "Опциональное")]
    Optional,

    [Display(Name = "Рекомендованное",
        Description = "При незаполненном поле будет выдаваться предупреждение, а заявка или персонаж — помечаться как проблемные")]
    Recommended,

    [Display(Name = "Обязательное",
        Description = "Сохранение с незаполенным полем будет невозможно.")]
    Required,
}
