using System.ComponentModel.DataAnnotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.Plots;

namespace JoinRpg.Web.Models.Print;

public class HandoutReportViewModel(IEnumerable<PlotElement> elements, IReadOnlyCollection<Character> characters)
{
    public IEnumerable<HandoutReportItemViewModel> Handouts { get; } = elements.Select(e => new HandoutReportItemViewModel(e, characters));
}

public class HandoutListItemViewModel(string text)
{
    [Display(Name = "Что раздавать")]
    public string Text { get; } = text;

    public HandoutListItemViewModel(PlotTextDto plotTextDto) : this(plotTextDto.Content.ToPlainText()) { }
}

public class HandoutReportItemViewModel : HandoutListItemViewModel
{
    public HandoutReportItemViewModel(PlotElement element, IReadOnlyCollection<Character> characters)
      : base(element.LastVersion().Content.ToPlainText().WithDefaultStringValue("(пустой текст)"))
    {
        PlotElementId = element.PlotElementId;
        PlotFolderId = element.PlotFolderId;
        ProjectId = element.ProjectId;
        Count = element.CountCharacters(characters);
        Status = element.GetStatus();
    }


    public int PlotElementId { get; }
    public int PlotFolderId { get; }
    public int ProjectId { get; }
    [Display(Name = "Количество")]
    public int Count { get; }
    public PlotStatus Status { get; }
}
