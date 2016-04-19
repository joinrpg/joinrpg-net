using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Plot
{
  public class EditPlotFolderViewModel : PlotFolderViewModelBase
  {
    public int PlotFolderId { get; set; }
    public IEnumerable<EditPlotElementViewModel> Elements { get; set; }

    public static EditPlotFolderViewModel FromFolder(PlotFolder folder)
    {
      return new EditPlotFolderViewModel()
      {
        PlotFolderMasterTitle = folder.MasterTitle,
        PlotFolderId = folder.PlotFolderId,
        TodoField = folder.TodoField,
        ProjectId = folder.ProjectId,
        Elements = folder.Elements. Select(FromElement).OrderBy(e => e.Status),
        Status = GetStatus(folder)
      };
    }

    private static EditPlotElementViewModel FromElement(PlotElement e)
    {
      return new EditPlotElementViewModel()
      {
        PlotElementId = e.PlotElementId,
        Targets = e.GetElementBindingsForEdit(),
        TargetsForDisplay = e.GetTargets().AsObjectLinks().ToList(),
        Content =new MarkdownViewModel(e.Content),
        TodoField = e.TodoField,
        ProjectId = e.PlotFolder.ProjectId,
        PlotFolderId = e.PlotFolderId,
        Status = GetStatus(e),
        IsCompleted = e.IsCompleted,
        RootGroupId = e.Project.RootGroup.CharacterGroupId
      };
    }
  }

  public class EditPlotElementViewModel  : IRootGroupAware
  {
    [ReadOnly(true)]
    public int ProjectId { get; set; }
    [ReadOnly(true)]
    public int RootGroupId { get; set; }
    [ReadOnly(true)]
    public int PlotFolderId { get; set; }
    [ReadOnly(true)]
    public int PlotElementId { get; set; }


    [Display(Name="Для кого")]
    public IEnumerable<string> Targets {  get; set;}
    [Display(Name = "Текст вводной")]
    public MarkdownViewModel Content { get; set; }

    [Display(Name = "TODO (что доделать для мастеров)"), DataType(DataType.MultilineText)]
    public string TodoField { get; set; }

    [ReadOnly(true), Display(Name = "Статус")]
    public PlotStatus Status { get; set; }

    [Display(Name="Готов", Description = "Готовые загрузы показываются игрокам")]
    public bool IsCompleted { get; set; }

    [ReadOnly(true)]
    public IEnumerable<GameObjectLinkViewModel> TargetsForDisplay  { get; set; }
  }
}
