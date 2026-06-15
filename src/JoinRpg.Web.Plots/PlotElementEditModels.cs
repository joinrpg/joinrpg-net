using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace JoinRpg.Web.Plots;

[JsonPolymorphic]
[JsonDerivedType(typeof(PlotElementCreateViewModel), "create")]
[JsonDerivedType(typeof(PlotElementEditViewModel), "edit")]
public abstract class PlotElementViewModelBase
{
    [ReadOnly(true)]
    public int ProjectId { get; set; }

    [Display(Name = "Сюжет", Description = "Сюжет выступает в роли папки для вводных")]
    public int? PlotFolderId { get; set; }

    [Display(Name = "Текст"), Required]
    public string Content { get; set; } = "";

    [Display(Name = "TODO (что доделать для мастеров)"), DataType(DataType.MultilineText)]
    public string TodoField { get; set; } = "";

    [Display(Name = "Привязка к персонажам")]
    public CharacterIdentification[] TargetCharacters { get; set; } = [];

    [Display(Name = "Привязка к группам")]
    public CharacterGroupIdentification[] TargetGroups { get; set; } = [];

    [Display(Name = "Тип")]
    public PlotElementTypeView ElementType { get; set; }
}

public class PlotElementCreateViewModel : PlotElementViewModelBase
{
    [ReadOnly(true)]
    public required bool HasPlotEditAccess { get; set; }

    [Display(Name = "Тип", Description = "Тип элемента можно выбрать только при создании.")]
    public new PlotElementTypeView ElementType { get; set; }

    [Display(Name = "Сразу опубликовать", Description = "Публиковать можно только законченную вводную (с пустым TODO) и заполненной привязкой.")]
    public bool PublishNow { get; set; }

    public static string GetDefaultContent() => "# Заголовок вводной\n\n---\n";
}

public class PlotElementEditViewModel : PlotElementViewModelBase
{
    [ReadOnly(true)]
    public int PlotElementId { get; set; }

    [ReadOnly(true), Display(Name = "Статус")]
    public PlotStatus Status { get; set; }

    [ReadOnly(true)]
    public bool HasManageAccess { get; set; }

    [ReadOnly(true)]
    public bool HasPublishedVersion { get; set; }

    [ReadOnly(true)]
    public TargetsInfo? Target { get; set; }
    public string? PlotFolderName { get; set; }
}
