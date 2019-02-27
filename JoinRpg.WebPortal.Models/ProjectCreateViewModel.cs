using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
        [Range(typeof(bool),
            "true",
            "true",
            ErrorMessage = "Согласитесь с правилами, чтобы продолжить")]
        public bool RulesApproved { get; set; }
    }
}
