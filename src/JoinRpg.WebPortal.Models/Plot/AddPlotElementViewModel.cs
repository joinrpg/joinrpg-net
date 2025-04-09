using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.Plots;

namespace JoinRpg.Web.Models.Plot;

public class AddPlotElementViewModel : IProjectIdAware
{
    [ReadOnly(true)]
    public int ProjectId { get; set; }
    [Display(Name = "Сюжет", Description = "Сюжет выступает в роли папки для вводных"), Required]
    public int? PlotFolderId { get; set; }
    [Display(Name = "Текст вводной"), UIHint("MarkdownString"), Required]
    public string Content { get; set; }

    [Display(Name = "TODO (что доделать для мастеров)"), DataType(DataType.MultilineText)]
    public string TodoField { get; set; }

    [Display(Name = "Для кого", Description = "Тех, кому предназначена эта вводная, можно добавить сейчас или позже.")]
    public IEnumerable<string> Targets { get; set; } = [];

    [Display(Name = "Тип"), ReadOnly(true)]
    public PlotElementTypeView ElementType { get; } = PlotElementTypeView.RegularPlot;

    [ReadOnly(true)]
    public required bool HasPlotEditAccess { get; set; }

    [Display(Name = "Сразу опубликовать", Description = "Публиковать можно только законченную вводную (с пустым TODO) и заполненной привязкой.")]
    public bool PublishNow { get; set; }
}

public class AddPlotHandoutViewModel : IProjectIdAware
{
    [ReadOnly(true)]
    public int ProjectId { get; set; }

    [Display(Name = "Сюжет", Description = "Сюжет выступает в роли папки для вводных"), Required]
    public int PlotFolderId { get; set; }
    [Display(Name = "Что выдать")]
    public string Content { get; set; }

    [Display(Name = "TODO (что доделать для мастеров)"), DataType(DataType.MultilineText)]
    public string TodoField { get; set; }

    [Display(Name = "Для кого", Description = "Тех, кому предназначен этот элемент раздатки, можно добавить сейчас или позже.")]
    public IEnumerable<string> Targets { get; set; } = [];
    public string PlotFolderName { get; set; }

    [Display(Name = "Тип"), ReadOnly(true)]
    public PlotElementTypeView ElementType { get; } = PlotElementTypeView.Handout;
}
