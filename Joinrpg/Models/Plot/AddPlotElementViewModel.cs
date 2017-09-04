using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models.Plot
{

  public class AddPlotElementViewModel : IProjectIdAware
  {
    [ReadOnly(true)]
    public int ProjectId { get; set; }
    public int PlotFolderId{ get; set; }
    [Display(Name = "Текст вводной"), UIHint("MarkdownString"), Required]
    public string Content { get; set; }

    [Display(Name = "TODO (что доделать для мастеров)"), DataType(DataType.MultilineText)]
    public string TodoField { get; set; }

    [Display(Name = "Для кого", Description = "Тех, кому предназначена эта вводная, можно добавить сейчас или позже.")]
    public IEnumerable<string> Targets { get; set; } = new string[] {};
    public string PlotFolderName { get; set; }

    [Display(Name = "Тип"), ReadOnly(true)]
    public PlotElementTypeView ElementType { get; } = PlotElementTypeView.RegularPlot;
  }

  public class AddPlotHandoutViewModel : IProjectIdAware
  {
    [ReadOnly(true)]
    public int ProjectId { get; set; }
    public int PlotFolderId { get; set; }
    [Display(Name = "Что выдать")]
    public string Content { get; set; }

    [Display(Name = "TODO (что доделать для мастеров)"), DataType(DataType.MultilineText)]
    public string TodoField { get; set; }

    [Display(Name = "Для кого", Description = "Тех, кому предназначен этот элемент раздатки, можно добавить сейчас или позже.")]
    public IEnumerable<string> Targets { get; set; } = new string[] { };
    public string PlotFolderName { get; set; }

    [Display(Name = "Тип"), ReadOnly(true)]
    public PlotElementTypeView ElementType { get; } = PlotElementTypeView.Handout;
  }


  public enum PlotElementTypeView
  {
    [Display(Name = "Обычная вводная", Description = "Текст, который нужно выдать игроку.")]
    RegularPlot,
    [Display(Name = "Элемент раздатки", Description = "Инструкция службе регистрации выдать какой-то определенный предмет игроку. Одна строка — один предмет.")]
    Handout
  }
}