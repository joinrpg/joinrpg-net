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
    public ProjectIdentification? CopyFromProjectId { get; set; }

    [Display(Name = "Глубина копирования")]
    public ProjectCopySettingsViewModel CopySettings { get; set; } = default;
}

public enum ProjectCopySettingsViewModel
{
    [Display(Name = "Настройки и поля", Description = "Будут перенесены настройки проекта, поля персонажа и заявки (с условных полей будет убрано условие).")]
    SettingsAndFields,
    [Display(Name = "Все выше + группы и шаблоны", Description = "Также будет перенесены группы персонажей с описанием, персонажи-шаблоны (кроме удаленных). Условия у полей будут сохранены. Заявки и сюжеты не будут перенесены. Этот вариант подходит для копирования конвентов.")]
    SettingsFieldsGroupsAndTemplates,
    [Display(Name = "Все выше + персонажи", Description = "Также будут перенесены все персонажи, кроме удаленных (не только шаблоны). Заявки и сюжеты не будут перенесены.")]
    SettingsFieldsGroupsAndCharacters,
    [Display(Name = "Все выше + сюжеты", Description = "Также будет перенесены сюжеты и раздатки. Заявки не будут перенесены. Этот вариант подходит, чтобы перепоставить коробочную игру.")]
    SettingsFieldsGroupsCharactersAndPlot,
}

public enum ProjectTypeViewModel
{
    [Display(Name = "Ролевая игра", Description = "Рекомендованные настройки: поля «имя» и «описание».")]
    Larp,

    [Display(Name = "Конвент: участники", Description = "Рекомендованные настройки: имя персонажа привязано к имени игрока, включена система поселения, автопринятие заявок.")]
    Convention,

    [Display(Name = "Конвент: мероприятия", Description = "Рекомендованные настройки: поля «название» и «описание», можно отправлять несколько заявок, включена система расписания.")]
    ConventionProgram,

    [Display(Name = "Скопировать настройки с другого проекта", Description = "Глубину копирования можно выбрать")]
    CopyFromAnother,

    [Display(Name = "Нет настроек", Description = "В проекте не установлены никакие настройки, все надо будет делать с нуля и вручную. Не рекомендуется.")]
    EmptyProject
}
