using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Plot
{
  public class PlotElementViewModel
  {
    public PlotStatus Status { get; set; }
    public MarkdownString Content { get; set; }
    public int PlotFolderId { get; set; }
    public int ProjectId { get; set; }
    public bool HasMasterAccess { get; set; }

    public static PlotElementViewModel FromPlotElement(PlotElement p, bool hasMasterAccess)
    {
      return new PlotElementViewModel
      {
        Content = p.Content,
        HasMasterAccess = hasMasterAccess,
        PlotFolderId = p.PlotFolderId,
        ProjectId = p.ProjectId,
        Status = PlotFolderViewModelBase.GetStatus(p)
      };
    }
  }
}
