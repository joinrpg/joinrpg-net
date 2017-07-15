using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers.Web;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models.Print
{
  public class HandoutReportViewModel
  {
    public HandoutReportViewModel(IEnumerable<PlotElement> elements, IReadOnlyCollection<Character> characters)
    {
      Handouts = elements.Select(e => new HandoutReportItemViewModel(e, characters));
    }
    public IEnumerable<HandoutReportItemViewModel> Handouts { get; }
  }

  public class HandoutViewModelBase 
  {
    public HandoutViewModelBase(IHtmlString text, User master)
    {
      Text = text;
      Master = master;
    }

    [Display(Name = "Что раздавать")]
    public IHtmlString Text { get; }

    [Display(Name="Мастер")]
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
}