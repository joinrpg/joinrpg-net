using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectCommon.Projects;

public class ProjectCreateViewModel
{
    [DisplayName("Название игры"), Required,
     StringLength(60,
         ErrorMessage = "Название игры должно быть длиной от 5 до 60 букв.",
         MinimumLength = 5)]
    public string ProjectName { get; set; }

    [Display(Name = "Согласен с правилами")]
    [BooleanRequired(ErrorMessage = "Согласитесь с правилами, чтобы продолжить")]
    public bool RulesApproved { get; set; }

    [Required]
    [Display(Name = "Тип проекта",
     Description = "Выберите, что лучше описывает ваш проект и мы сразу настроим его наиболее оптимально для вас. Не бойтесь, вы всегда сможете изменить конкретные настройки позже.")]
    public ProjectTypeViewModel ProjectType { get; set; }
}

public enum ProjectTypeViewModel
{
    [Display(Name = "Ролевая игра",
        Description = "")]
    Larp,
    [Display(Name = "Конвент - участники",
        Description = "")]
    Convention,

    [Display(Name = "Конвент - мероприятия")]
    ConventionProgram
}
