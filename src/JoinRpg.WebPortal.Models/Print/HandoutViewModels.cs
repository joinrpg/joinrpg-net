using System.ComponentModel.DataAnnotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.Plots;

namespace JoinRpg.Web.Models.Print;

public class HandoutReportViewModel
{
    public HandoutReportViewModel(IReadOnlyDictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>> handoutsDict)
    {
        var dict = new Dictionary<PlotTextDto, int>();
        foreach (var pair in handoutsDict)
        {
            foreach (var handout in pair.Value)
            {
                if (dict.TryGetValue(handout, out var value))
                {
                    dict[handout] = value + 1;
                }
                else
                {
                    dict[handout] = 1;
                }

            }
        }
        Handouts = dict.Select(pair => new HandoutReportItemViewModel(pair.Key, pair.Value));
    }

    public IEnumerable<HandoutReportItemViewModel> Handouts { get; }
}

public class HandoutListItemViewModel(PlotTextDto plotTextDto)
{
    [Display(Name = "Что раздавать")]
    public string Text { get; } = plotTextDto.Content.ToPlainTextWithoutHtmlEscape();
}

public class HandoutReportItemViewModel(PlotTextDto element, int count)
    : HandoutListItemViewModel(element)
{
    public PlotElementIdentification PlotElementId { get; } = element.Id.PlotElementId;
    [Display(Name = "Количество")]
    public int Count { get; } = count;
    public PlotStatus Status { get; } = element.GetStatus();
}
