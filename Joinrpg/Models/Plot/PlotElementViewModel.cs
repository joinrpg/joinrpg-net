using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Plot
{
  public class PlotElementViewModel : IMovableListItem
  {
    public PlotStatus Status { get; }
    public MarkdownString Content { get; }
    public string TodoField{ get; }
    public int PlotFolderId { get; }

    public int PlotElementId { get; }
    public int ProjectId { get; }
    public bool HasMasterAccess { get; }

    public bool First { get; set; }
    public bool Last { get; set; }

    public bool Visible => Status == PlotStatus.Completed || (HasMasterAccess && Status == PlotStatus.InWork);

    public IEnumerable<GameObjectLinkViewModel> TargetsForDisplay { get; }

    public PlotElementViewModel (PlotElement p, bool hasMasterAccess, int? characterId)
    {
      Content = p.Texts.Content;
      TodoField = p.Texts.TodoField;
      HasMasterAccess = hasMasterAccess;
      PlotFolderId = p.PlotFolderId;
      PlotElementId = p.PlotElementId;
      ProjectId = p.ProjectId;
      Status = PlotFolderViewModelBase.GetStatus(p);
      TargetsForDisplay = p.GetTargets().AsObjectLinks().ToList();
      CharacterId = characterId;
    }

    int IMovableListItem.ItemId => PlotElementId;
    public int? CharacterId { get; private set; }

    public bool HasWorkTodo
    {
      get { return !string.IsNullOrWhiteSpace(TodoField) || Status == PlotStatus.InWork; }
    }
  }

  public static class PlotElementViewModelExtensions
  {
    public static IEnumerable<PlotElementViewModel> ToViewModels(this IEnumerable<PlotElement> plots,
      bool hasMasterAccess, int? characterId = null)
    {
      return plots.Where(p => p.ElementType == PlotElementType.RegularPlot).Select(
        p => new PlotElementViewModel(p, hasMasterAccess, characterId)).MarkFirstAndLast();
    }
  }
}
