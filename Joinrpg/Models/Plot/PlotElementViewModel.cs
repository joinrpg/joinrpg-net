using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Plot
{
  public class PlotElementViewModel : IMovableListItem
  {
    public PlotStatus Status { get; set; }
    public MarkdownString Content { get; set; }
    public int PlotFolderId { get; set; }

    public int PlotElementId { get; set; }
    public int ProjectId { get; set; }
    public bool HasMasterAccess { get; set; }

    public bool First { get; set; }
    public bool Last { get; set; }

    public static PlotElementViewModel FromPlotElement(PlotElement p, bool hasMasterAccess)
    {
      return new PlotElementViewModel
      {
        Content = p.Content,
        HasMasterAccess = hasMasterAccess,
        PlotFolderId = p.PlotFolderId,
        PlotElementId = p.PlotElementId,
        ProjectId = p.ProjectId,
        Status = PlotFolderViewModelBase.GetStatus(p)
      };
    }
  }

  public static class PlotElementViewModelExtensions
  {
    public static IEnumerable<PlotElementViewModel> ToViewModels(this IEnumerable<PlotElement> plots, bool hasMasterAccess)
    {
      return plots.Select(
        p => PlotElementViewModel.FromPlotElement(p, hasMasterAccess)).ToList().MarkFirstAndLast();
    }
  }
}
