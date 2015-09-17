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
    public IEnumerable<EditPlotElementViewModel> Elements { get; set; }

    public static EditPlotFolderViewModel FromFolder(PlotFolder folder)
    {
      var groupsView = CharacterGroupListViewModel.FromGroupAsMaster(folder.Project.RootGroup);
      return new EditPlotFolderViewModel()
      {
        PlotFolderMasterTitle = folder.MasterTitle,
        PlotFolderId = folder.PlotFolderId,
        TodoField = folder.TodoField,
        ProjectId = folder.ProjectId,
        Elements = folder.Elements. Select(e => FromElement(e, groupsView)).OrderBy(e => e.Status),
        Status = GetStatus(folder)
      };
    }

    private static EditPlotElementViewModel FromElement(PlotElement e, CharacterGroupListViewModel groupsView)
    {
      return new EditPlotElementViewModel()
      {
        PlotElementId = e.PlotElementId,
        For = e.Targets.Select(t => t.AsObjectLink()),
        Content = e.Content,
        TodoField = e.TodoField,
        ProjectId = e.PlotFolder.ProjectId,
        PlotFolderId = e.PlotFolderId,
        Status = GetStatus(e),
        Data = groupsView,
        IsCompleted = e.IsCompleted
      };
    }
  }

  public class EditPlotElementViewModel  : IEditablePlotElementViewModel
  {
    public int ProjectId { get; set; }

    public int PlotFolderId { get; set; }

    public int PlotElementId { get; set; }


    [Display(Name="Для кого")]
    public IEnumerable<GameObjectLinkViewModel> For {  get; set;}
    [Display(Name = "Текст вводной")]
    public MarkdownString Content { get; set; }

    [Display(Name = "TODO (что доделать для мастеров)"), DataType(DataType.MultilineText)]
    public string TodoField { get; set; }

    [ReadOnly(true), Display(Name = "Статус")]
    public PlotStatus Status { get; set; }

    [Display(Name="Готов", Description = "Готовые загрузы показываются игрокам")]
    public bool IsCompleted { get; set; }

    [ReadOnly(true)]
    public CharacterGroupListViewModel Data { get; set; }
  }
}
