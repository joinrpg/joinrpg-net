using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Plot
{
  public class PlotElementViewModel : IMovableListItem
  {
    public PlotStatus Status { get; }
    public MarkdownString Content { get; }
    public int PlotFolderId { get; }

    public int PlotElementId { get; }
    public int ProjectId { get; }
    public bool HasMasterAccess { get; }

    public bool First { get; set; }
    public bool Last { get; set; }

    public bool Visible => Status == PlotStatus.Completed || (HasMasterAccess && Status == PlotStatus.InWork);

    public PlotElementViewModel (PlotElement p, bool hasMasterAccess, int? characterId)
    {
      Content = p.Texts.Content;
      HasMasterAccess = hasMasterAccess;
      PlotFolderId = p.PlotFolderId;
      PlotElementId = p.PlotElementId;
      ProjectId = p.ProjectId;
      Status = PlotFolderViewModelBase.GetStatus(p);
      CharacterId = characterId;
    }

    int IMovableListItem.ItemId => PlotElementId;
    public int? CharacterId { get; private set; }
  }

  public static class PlotElementViewModelExtensions
  {
    public static IEnumerable<PlotElementViewModel> ToViewModels(this IEnumerable<PlotElement> plots,
      bool hasMasterAccess, int? characterId = null)
    {
      return plots.Select(
        p => new PlotElementViewModel(p, hasMasterAccess, characterId)).MarkFirstAndLast();
    }
  }
}
