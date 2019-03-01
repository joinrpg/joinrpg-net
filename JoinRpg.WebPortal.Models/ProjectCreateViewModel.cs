using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using JoinRpg.Helpers.Web;

namespace JoinRpg.Web.Models
{
    public class ProjectCreateViewModel
    {
        [DisplayName("Название игры"), Required,
         StringLength(100,
             ErrorMessage = "Название игры должно быть длиной от 5 до 100 букв.",
             MinimumLength = 5)]
        public string ProjectName { get; set; }

        [Required]
        [Display(Name = "Согласен с правилами")]
        [BooleanRequired(ErrorMessage = "Согласитесь с правилами, чтобы продолжить")]
        public bool RulesApproved { get; set; }

        [Required]
        [Display(Name = "Тип проекта",
         Description = "Выберите, что лучше описывает ваш проект и мы сразу настроим его наиболее оптимально для вас. Не бойтесь, вы всегда сможете изменить конкретные настройки позже.")]
        public ProjectTypeViewModel ProjectType { get; set; }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    public enum ProjectTypeViewModel
    {
        [Display(Name="Ролевая игра",
            Description = "")]
        Larp,
        [Display(Name = "Конвент",
            Description = "")]
        Convention
    }
}
