using System.Collections.Generic;

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
    public IEnumerable<GameObjectLinkViewModel> For {  get; set;}

  }
}
