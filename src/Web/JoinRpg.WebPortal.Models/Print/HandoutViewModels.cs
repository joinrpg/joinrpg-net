using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.Plots;

namespace JoinRpg.Web.Models.Print;

/// <summary>
/// Отчёт по раздаткам. Строки идут в порядке сюжетов — так их готовит
/// <c>HandoutReportViewModelBuilder</c>, здесь порядок только сохраняется.
/// </summary>
public class HandoutReportViewModel(IReadOnlyList<HandoutReportItemViewModel> handouts)
{
    public IReadOnlyList<HandoutReportItemViewModel> Handouts { get; } = handouts;
}

public class HandoutListItemViewModel(PlotTextDto plotTextDto)
{
    [Display(Name = "Что раздавать")]
    public string Text { get; } = ((MarkdownString?)plotTextDto.Content).ToPlainTextWithoutHtmlEscape();
}

public class HandoutReportItemViewModel(PlotTextDto element, int count)
    : HandoutListItemViewModel(element)
{
    public PlotElementIdentification PlotElementId { get; } = element.Id.PlotElementId;
    [Display(Name = "Количество")]
    public int Count { get; } = count;
    public PlotStatus Status { get; } = element.GetStatus();
}
