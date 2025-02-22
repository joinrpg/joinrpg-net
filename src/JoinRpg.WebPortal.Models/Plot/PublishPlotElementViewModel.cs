using System.ComponentModel.DataAnnotations;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Plots;

namespace JoinRpg.Web.Models.Plot;

public class PublishPlotElementViewModel
{

    public int ProjectId { get; set; }

    public int PlotFolderId { get; set; }

    public int PlotElementId { get; set; }

    public int Version { get; set; }

    [Display(Name = "Отправить уведомление всем ассоциированным игрокам")]
    public bool SendNotification { get; set; } = true;

    [Display(Name = "Комментарий")]
    [UIHint("MarkdownString")]
    [DataType(DataType.MultilineText)]
    public string CommentText { get; set; }

    public PublishPlotElementViewModel()
    {
    }

    public PublishPlotElementViewModel(EditPlotFolderViewModel source)
    {
        ProjectId = source.ProjectId;
        PlotFolderId = source.PlotFolderId;
    }

    public PlotVersionIdentification GetVersionId() => new PlotVersionIdentification(new PlotElementIdentification(new PlotFolderIdentification(
        new ProjectIdentification(ProjectId), PlotFolderId), PlotElementId), Version);
}
