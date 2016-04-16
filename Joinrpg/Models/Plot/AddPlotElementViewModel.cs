using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Plot
{

  public class AddPlotElementViewModel : IRootGroupAware
  {
    [ReadOnly(true)]
    public int ProjectId { get; set; }
    [ReadOnly(true)]
    public int RootGroupId { get; set; }
    public int PlotFolderId{ get; set; }
    [Display(Name = "Текст вводной")]
    public MarkdownViewModel Content { get; set; }

    [Display(Name = "TODO (что доделать для мастеров)"), DataType(DataType.MultilineText)]
    public string TodoField { get; set; }

    [Display(Name = "Для кого", Description = "Тех, кому предназначена эта вводная, можно добавить сейчас или позже.")]
    public IEnumerable<string> Targets { get; set; } = new string[] {};
    public string PlotFolderName { get; set; }
  }
}