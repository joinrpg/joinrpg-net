using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Plot
{
  public class PlotElementViewModel : IMovableListItem
  {
    public PlotStatus Status { get; }
    public IHtmlString Content { get; }
    public string TodoField{ get; }
    public int PlotFolderId { get; }

    public int PlotElementId { get; }
    public int ProjectId { get; }
    public bool HasMasterAccess { get; }

    public bool HasEditAccess { get; }

    public bool First { get; set; }
    public bool Last { get; set; }

    public bool Visible => Status == PlotStatus.Completed || (HasMasterAccess && Status == PlotStatus.InWork);

    public IEnumerable<GameObjectLinkViewModel> TargetsForDisplay { get; }

    public PlotElementViewModel([NotNull] PlotElement p, [CanBeNull] Character character, int? currentUserId, ILinkRenderer linkRendrer)
    {
      if (p == null) throw new ArgumentNullException(nameof(p));

      Content = p.LastVersion().Content.ToHtmlString(linkRendrer);
      TodoField = p.LastVersion().TodoField;
      HasMasterAccess = p.HasMasterAccess(currentUserId);
      HasEditAccess = p.HasMasterAccess(currentUserId) && p.Project.Active;
      HasPlayerAccess = character?.HasPlayerAccess(currentUserId) ?? false;
      PlotFolderId = p.PlotFolderId;
      PlotElementId = p.PlotElementId;
      ProjectId = p.ProjectId;
      Status = p.GetStatus();
      TargetsForDisplay = p.GetTargets().AsObjectLinks().ToList();
      CharacterId = character?.CharacterId;
      PublishMode = !HasMasterAccess && !HasPlayerAccess;
    }

    public bool PublishMode { get; }

    public bool HasPlayerAccess { get; }

    int IMovableListItem.ItemId => PlotElementId;
    public int? CharacterId { get; private set; }

    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(TodoField) || Status == PlotStatus.InWork;
  }

  public static class PlotElementViewModelExtensions
  {
    public static IEnumerable<PlotElementViewModel> ToViewModels([NotNull] this IEnumerable<PlotElement> plots, int? currentUserId, [CanBeNull] Character character = null)
    {
      if (plots == null) throw new ArgumentNullException(nameof(plots));

      return plots.Where(p => p.ElementType == PlotElementType.RegularPlot).Select(
        p => new PlotElementViewModel(p, character, currentUserId, new JoinrpgMarkdownLinkRenderer(p.Project))).MarkFirstAndLast();
    }
  }
}
