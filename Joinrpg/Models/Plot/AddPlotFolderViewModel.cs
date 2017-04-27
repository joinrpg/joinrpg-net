using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models.Plot
{
  public class AddPlotFolderViewModel : PlotFolderViewModelBase
  {
    [ReadOnly(true)]
    public string ProjectName { get; set; }


    [Required, Display(Name = "Название сюжета", Description = "Вы можете указать теги прямо в названии. Пример: #мордор #гондор #костромская_область")]
    public string PlotFolderTitleAndTags { get; set; }
  }
}
