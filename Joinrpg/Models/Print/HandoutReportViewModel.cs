using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models.Print
{
  public class HandoutReportViewModel
  {
    public HandoutReportViewModel(IEnumerable<PlotElement> elements, IReadOnlyCollection<Character> characters)
    {
      Handouts = elements.Select(e => new HandoutReportItemViewModel(e, characters));
    }
    public IEnumerable<HandoutReportItemViewModel> Handouts { get; private set; }
  }

  public class HandoutReportItemViewModel
  {
    public HandoutReportItemViewModel(PlotElement element, IReadOnlyCollection<Character> characters)
    {
      Text = element.LastVersion().Content.ToPlainText().WithDefaultStringValue("(пустой текст)");
      PlotElementId = element.PlotElementId;
      PlotFolderId = element.PlotFolderId;
      ProjectId = element.ProjectId;
      Count = element.CountCharacters(characters);
      Status = element.GetStatus();
    }

    [Display(Name="Что раздавать")]
    public string Text { get; private set; }
    public int PlotElementId { get; private set; }
    public int PlotFolderId { get; private set; }
    public int ProjectId { get; private set; }
    [Display(Name = "Количество")]
    public int Count { get; private set; }
    public PlotStatus Status { get; }
  }
}