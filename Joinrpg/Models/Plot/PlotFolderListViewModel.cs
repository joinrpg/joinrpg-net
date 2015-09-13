using System.Collections.Generic;
using System.ComponentModel;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Plot
{
  [ReadOnly(true)]
  public class PlotFolderListViewModel
  {
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }

    //TODO: May be better to have ViewModel here
    public IEnumerable<PlotFolder> Folders;
  }
}
