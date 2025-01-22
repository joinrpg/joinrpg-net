using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectCommon.Projects;

public class ProjectCreateViewModel
{
    [DisplayName("Название игры"), Required,
     StringLength(60,
         ErrorMessage = "Название игры должно быть длиной от 5 до 60 букв.",
         MinimumLength = 5)]
    public string ProjectName { get; set; } = default!;

    [Display(Name = "Согласен с правилами")]
    [BooleanRequired(ErrorMessage = "Согласитесь с правилами, чтобы продолжить")]
    public bool RulesApproved { get; set; }

    [Required]
    [Display(Name = "Тип проекта")]
    public ProjectTypeViewModel ProjectType { get; set; }

    [Display(Name = "Откуда копировать", Description = "Из этого проекта будут перенесены все поля и настройки")]
    public int? CopyFromProjectId { get; set; }
}

public enum ProjectTypeViewModel
{
    [Display(Name = "Ролевая игра", Description = "Рекомендованные настройки: поля «имя» и «описание».")]
    Larp,

    [Display(Name = "Конвент: участники", Description = "Рекомендованные настройки: имя персонажа привязано к имени игрока, включена система поселения, автопринятие заявок.")]
    Convention,

    [Display(Name = "Конвент: мероприятия", Description = "Рекомендованные настройки: поля «название» и «описание», можно отправлять несколько заявок, включена система расписания.")]
    ConventionProgram,

    [Display(Name = "Скопировать настройки с другого проекта", Description = "Будут перенесены настройки и поля. Персонажи, взнос и сюжет перенесены не будут.")]
    CopyFromAnother,

    [Display(Name = "Нет настроек", Description = "В проекте не установлены никакие настройки, все надо будет делать с нуля и вручную. Не рекомендуется.")]
    EmptyProject
}
