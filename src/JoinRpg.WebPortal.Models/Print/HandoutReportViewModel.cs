using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models.Print;

public class HandoutReportViewModel(IEnumerable<PlotElement> elements, IReadOnlyCollection<Character> characters)
{
    public IEnumerable<HandoutReportItemViewModel> Handouts { get; } = elements.Select(e => new HandoutReportItemViewModel(e, characters));
}

public class HandoutViewModelBase
{
    public HandoutViewModelBase(string text, User master)
    {
        Text = text;
        Master = master;
    }

    [Display(Name = "Что раздавать")]
    public string Text { get; }

    [Display(Name = "Мастер")]
    public User Master { get; }
}

public class HandoutReportItemViewModel : HandoutViewModelBase
{
    public HandoutReportItemViewModel(PlotElement element, IReadOnlyCollection<Character> characters)
      : base(element.LastVersion().Content.ToPlainText().WithDefaultStringValue("(пустой текст)"),
          element.LastVersion().AuthorUser)
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
