using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Web.Helpers;

namespace JoinRpg.Web.Models.Plot
{
  public class EditPlotFolderViewModel : PlotFolderViewModelBase
  {
    public int PlotFolderId { get; set; }
    public IEnumerable<PlotElementListItemViewModel> Elements { get; set; }

    public static EditPlotFolderViewModel FromFolder(PlotFolder folder)
    {
      return new EditPlotFolderViewModel()
      {
        PlotFolderMasterTitle = folder.MasterTitle,
        PlotFolderId = folder.PlotFolderId,
        TodoField = folder.TodoField,
        ProjectId = folder.ProjectId,
        Elements = folder.Elements.Select(e => new PlotElementListItemViewModel()
        {
          PlotFolderElementId = e.PlotElementId,
          For = e.Targets.Select(t => t.AsObjectLink())
        }),
        Status = GetStatus(folder)
      };
    }
  }

  public class PlotElementListItemViewModel 
  {
    public int PlotFolderElementId { get; set; }
    [Display(Name="Для кого")]
    public IEnumerable<GameObjectLinkViewModel> For {  get; set;}

  }
}
