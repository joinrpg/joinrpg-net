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
    public IEnumerable<EditPlotElementViewModel> Elements { get; private set; }

    public bool HasEditAccess { get; private set; }

    public EditPlotFolderViewModel (PlotFolder folder, int? currentUserId)
    {
      PlotFolderMasterTitle = folder.MasterTitle;
      PlotFolderId = folder.PlotFolderId;
      TodoField = folder.TodoField;
      ProjectId = folder.ProjectId;
      Elements = folder.Elements.Select(e => new EditPlotElementViewModel(e, currentUserId)).OrderBy(e => e.Status);
      Status = folder.GetStatus();
      HasEditAccess = folder.HasMasterAccess(currentUserId, acl => acl.CanManagePlots) && folder.Project.Active;
      HasMasterAccess = folder.HasMasterAccess(currentUserId);
    }

    public bool HasMasterAccess { get; }
  }

  public class EditPlotElementViewModel  : IProjectIdAware
  {

    public EditPlotElementViewModel(PlotElement e, int? currentUserId)
    {
      PlotElementId = e.PlotElementId;
      Targets = e.GetElementBindingsForEdit();
      TargetsForDisplay = e.GetTargets().AsObjectLinks().ToList();
      Content = new MarkdownViewModel(e.Texts.Content);
      TodoField = e.Texts.TodoField;
      ProjectId = e.PlotFolder.ProjectId;
      PlotFolderId = e.PlotFolderId;
      Status = e.GetStatus();
      IsCompleted = e.IsCompleted;
      ElementType = (PlotElementTypeView) e.ElementType;
      HasEditAccess = e.PlotFolder.HasMasterAccess(currentUserId, acl => acl.CanManagePlots) && e.Project.Active;
      HasMasterAccess = e.PlotFolder.HasMasterAccess(currentUserId);
    }

    [ReadOnly(true)]
    public int ProjectId { get; }
    [ReadOnly(true)]
    public int PlotFolderId { get; }
    [ReadOnly(true)]
    public int PlotElementId { get; }


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
    public IEnumerable<GameObjectLinkViewModel> TargetsForDisplay  { get; }

    public PlotElementTypeView ElementType { get; }
    public bool HasEditAccess { get; }

    public bool HasMasterAccess { get; }
  }
}
