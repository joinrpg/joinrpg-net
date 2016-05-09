using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models.Plot
{
  [ReadOnly(true)]
  public class PlotFolderListViewModel
  {
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }

    public IEnumerable<PlotFolderListItemViewModel> Folders;

    public static PlotFolderListViewModel FromProject(List<PlotFolder> folders, Project project)
    {
      return new PlotFolderListViewModel()
      {
        ProjectId = project.ProjectId,
        ProjectName = project.ProjectName,
        Folders =
          folders
            .Select(PlotFolderListItemViewModel.FromFolder)
            .OrderBy(pf => pf.Status)
            .ThenBy(pf => pf.PlotFolderMasterTitle)
      };
    }
  }

  public class PlotFolderListItemViewModel : PlotFolderViewModelBase
  {
    public int PlotFolderId { get; set; }
    public int ElementsCount { get; set; }
    public string ElementTodos { get; set; }

    public static PlotFolderListItemViewModel FromFolder(PlotFolder folder)
    {
      return new PlotFolderListItemViewModel
      {
        PlotFolderId = folder.PlotFolderId,
        PlotFolderMasterTitle = folder.MasterTitle,
        ProjectId = folder.ProjectId,
        Status = GetStatus(folder),
        ElementsCount = folder.Elements.Count(),
        ElementTodos = string.Join("\n", folder.Elements.Select(e => e.Texts.TodoField).WhereNotNullOrWhiteSpace()),
        TodoField = folder.TodoField
      };
    }
  }
}
