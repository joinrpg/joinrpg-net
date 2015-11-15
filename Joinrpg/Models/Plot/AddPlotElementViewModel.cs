using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Plot
{
  public interface IEditablePlotElementViewModel
  {
    CharacterGroupListViewModel Data { get; }
    IEnumerable<GameObjectLinkViewModel> For { get;  }
  }

  public class AddPlotElementViewModel : IEditablePlotElementViewModel
  {
    public int ProjectId { get; set; }
    public int PlotFolderId{ get; set; }
    [Display(Name = "Текст вводной")]
    public MarkdownViewModel Content { get; set; }

    [Display(Name = "TODO (что доделать для мастеров)"), DataType(DataType.MultilineText)]
    public string TodoField { get; set; }

    public CharacterGroupListViewModel Data { get; set; }
    public IEnumerable<GameObjectLinkViewModel> For => new GameObjectLinkViewModel[] {};
    public string PlotFolderName { get; set; }
  }
}