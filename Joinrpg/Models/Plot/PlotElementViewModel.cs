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

    public bool Visible => Status == PlotStatus.Completed || (HasMasterAccess && Status == PlotStatus.InWork);

    public static PlotElementViewModel FromPlotElement(PlotElement p, bool hasMasterAccess, int characterId)
    {
      return new PlotElementViewModel
      {
        Content = p.Texts.Content,
        HasMasterAccess = hasMasterAccess,
        PlotFolderId = p.PlotFolderId,
        PlotElementId = p.PlotElementId,
        ProjectId = p.ProjectId,
        Status = PlotFolderViewModelBase.GetStatus(p),
        CharacterId = characterId
      };
    }

    int IMovableListItem.ItemId => PlotElementId;
    public int CharacterId { get; private set; }
  }

  public static class PlotElementViewModelExtensions
  {
    public static IEnumerable<PlotElementViewModel> ToViewModels(this IEnumerable<PlotElement> plots,
      bool hasMasterAccess, int characterId)
    {
      return plots.Where(p => p.ElementType == PlotElementType.RegularPlot) .Select(
        p => PlotElementViewModel.FromPlotElement(p, hasMasterAccess, characterId)).MarkFirstAndLast();
    }
  }
}
