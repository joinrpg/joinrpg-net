using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Plots;

public class PublishPlotElementViewModel
{

    public required PlotVersionIdentification PlotVersionId { get; set; }

    public required ProjectIdentification ProjectId { get; set; }

    [Display(Name = "Отправить уведомление всем ассоциированным игрокам")]
    public bool SendNotification { get; set; } = true;

    [Display(Name = "Комментарий")]
    [UIHint("MarkdownString")]
    [DataType(DataType.MultilineText)]
    public string? CommentText { get; set; }
}
