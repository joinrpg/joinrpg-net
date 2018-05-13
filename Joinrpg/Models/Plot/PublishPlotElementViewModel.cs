using System.ComponentModel.DataAnnotations;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Plot
{

    public class PublishPlotElementViewModel : IPublishPlotElementModel
    {

        public int ProjectId { get; set; }

        public int PlotFolderId { get; set; }

        public int PlotElementId { get; set; }

        public int? Version { get; set; }

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
    }
}
