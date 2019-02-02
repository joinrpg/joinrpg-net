using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.Plot
{
  public class AddPlotFolderViewModel : PlotFolderViewModelBase, IValidatableObject
  {
    [ReadOnly(true)]
    public string ProjectName { get; set; }


    [Required, Display(Name = "Название сюжета", Description = "Вы можете указать теги прямо в названии. Пример: #мордор #гондор #костромская_область")]
    public string PlotFolderTitleAndTags { get; set; }

      public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
      {
          if (string.IsNullOrWhiteSpace(PlotFolderTitleAndTags.RemoveTagNames()))
          {
              yield return new ValidationResult("Необходимо указать название",
                  new[] {nameof(PlotFolderTitleAndTags)});
          }
      }
  }
}
