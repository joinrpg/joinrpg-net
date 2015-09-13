using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models.Plot
{
  public class EditPlotFolderViewModel : PlotFolderViewModelBase
  {
    public int PlotFolderId { get; set; }
    public IEnumerable<PlotElementListItemViewModel> Elements { get; set; }
  }

  public class PlotElementListItemViewModel 
  {
    public int PlotFolderElementId { get; set; }
    [Display(Name="Для кого")]
    public IEnumerable<GameObjectLinkViewModel> For {  get; set;}

  }
}
