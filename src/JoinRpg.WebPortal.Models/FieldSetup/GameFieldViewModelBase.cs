using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers.Web;

namespace JoinRpg.Web.Models.FieldSetup
{
    public abstract class GameFieldViewModelBase : IValidatableObject, IFieldNavigationAware
    {
        public int ProjectId { get; set; }

        [Display(Name = "Название поля"), Required]
        public string Name { get; set; }

        [Display(Name = "Публичное (видно всем)")]
        public bool IsPublic { get; set; }

        [Display(Name = "Видно игроку")]
        public bool CanPlayerView { get; set; }

        [Display(
            Name = "Игрок может менять",
            Description = "Для полей типа выбор/мультивыбор можно запретить выставление каждого значения в отдельности в свойствах значения")]
        public bool CanPlayerEdit { get; set; }

        [Display(Name = "Описание")]
        public JoinHtmlString DescriptionDisplay { get; set; }

        [Display(Name = "Описание (только для мастеров)")]
        public JoinHtmlString MasterDescriptionDisplay { get; set; }

        [Display(Name = "Обязательное?")]
        public MandatoryStatusViewType MandatoryStatus { get; set; }

        [Display(Name = "Показывать только для групп", Description = "Если оставить пустым, будет показываться всегда")]
        public ICollection<string> ShowForGroups { get; set; } = new List<string>();

        [Display(Name = "Доступно NPC", Description = "Доступно для персонажей-NPC")]
        public bool ValidForNpc { get; set; }

        [Display(Name = "Показывать даже при непринятой заявке")]
        public bool ShowForUnApprovedClaim { get; set; } = true;

        [Display(Name = "Цена", Description = "Цена будет добавлена ко взносу")]
        public int Price { get; set; } = 0;

        public FieldNavigationModel Navigation { get; set; }

        public abstract void SetNavigation(FieldNavigationModel navigationModel);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsPublic && !CanPlayerView)
            {
                yield return new ValidationResult("Нельзя скрыть публичное поле от игрока.",
                  new[] { nameof(CanPlayerView), nameof(IsPublic) });
            }
            if (CanPlayerEdit && !CanPlayerView)
            {
                yield return
                  new ValidationResult("Нельзя скрыть поле от игрока и одновременно разрешить редактирование поля.",
                    new[] { nameof(CanPlayerView), nameof(CanPlayerEdit) });
            }

            foreach (var validationResult in ValidateCore())
            {
                yield return validationResult;
            }
        }

        protected virtual IEnumerable<ValidationResult> ValidateCore() => Enumerable.Empty<ValidationResult>();
    }
}
