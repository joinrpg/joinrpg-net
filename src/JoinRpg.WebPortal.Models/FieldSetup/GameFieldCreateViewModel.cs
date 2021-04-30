using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models.FieldSetup
{
    public class GameFieldCreateViewModel : GameFieldViewModelBase
    {
        [Display(Name = "Тип поля")]
        public ProjectFieldViewType FieldViewType { get; set; }

        [Display(Name = "Описание"), UIHint("MarkdownString")]
        public string DescriptionEditable { get; set; }

        [Display(Name = "Описание (для мастеров)"), UIHint("MarkdownString")]
        public string MasterDescriptionEditable { get; set; }

        [Display(Name = "Привязано к")]
        public FieldBoundToViewModel FieldBoundTo { get; set; }

        public override void SetNavigation(FieldNavigationModel navigationModel)
        {
            navigationModel.Page = FieldNavigationPage.AddField;
            Navigation = navigationModel;
        }

        protected override IEnumerable<ValidationResult> ValidateCore()
        {
            if (FieldBoundTo == FieldBoundToViewModel.Claim && ValidForNpc)
            {
                yield return
                  new ValidationResult("Невозможно разрешить NPC поля, связанные с заявкой.",
                    new List<string> { nameof(DataModel.FieldBoundTo), nameof(ValidForNpc) });
            }
            if (Price != 0 && !FieldViewType.SupportsPricingOnField())
            {
                yield return new ValidationResult(
                    $"Поле {FieldViewType} не поддерживает ввод цены.");
            }
        }
    }
}
