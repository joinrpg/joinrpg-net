using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models.Plot
{
  public class AddPlotFolderViewModel : PlotFolderViewModelBase
  {
    [ReadOnly(true)]
    public string ProjectName { get; set; }
  }
}
