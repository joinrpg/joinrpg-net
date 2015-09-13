using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.DataModel;

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
