using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Print
{
  public class HandoutReportViewModel
  {
    public HandoutReportViewModel(IReadOnlyCollection<PlotElement> elements, IReadOnlyCollection<Character> characters)
    {
      Handouts = elements.Select(e => new HandoutReportItemViewModel(e, characters));
    }
    public IEnumerable<HandoutReportItemViewModel> Handouts { get; private set; }
  }

  public class HandoutReportItemViewModel
  {
    public HandoutReportItemViewModel(PlotElement element, IReadOnlyCollection<Character> characters)
    {
      Text = new MarkdownViewModel(element.Texts.Content);
      PlotElementId = element.PlotElementId;
      PlotFolderId = element.PlotFolderId;
      ProjectId = element.ProjectId;
      Count = element.CountCharacters(characters);
    }

    [Display(Name="Что раздавать")]
    public MarkdownViewModel Text { get; private set; }
    public int PlotElementId { get; private set; }
    public int PlotFolderId { get; private set; }
    public int ProjectId { get; private set; }
    [Display(Name = "Количество")]
    public int Count { get; private set; }
  }
}