using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Plot
{
  public abstract class PlotFolderListViewModelBase
  {
    protected PlotFolderListViewModelBase (Project project)
    {

      ProjectId = project.ProjectId;
      ProjectName = project.ProjectName;

    }

    public int ProjectId { get;  }
    public string ProjectName { get; }
   
  }

  [ReadOnly(true)]
  public class PlotFolderListViewModel : PlotFolderListViewModelBase
  {
    public IEnumerable<PlotFolderListItemViewModel> Folders { get; }

    public PlotFolderListViewModel (IEnumerable<PlotFolder> folders, Project project) : base(project)
    {
      Folders =
  folders
    .Select(f => new PlotFolderListItemViewModel(f))
    .OrderBy(pf => pf.Status)
    .ThenBy(pf => pf.PlotFolderMasterTitle);
    }
  }

  public class PlotFolderFullListViewModel : PlotFolderListViewModelBase
  {
    public IEnumerable<PlotFolderListFullItemViewModel> Folders { get; }
    public bool InWorkOnly { get; }

    public PlotFolderFullListViewModel(IEnumerable<PlotFolder> folders, Project project, bool inWorkOnly = false) : base(project)
    {
      InWorkOnly = inWorkOnly;
      Folders =
        folders
          .Select(f => new PlotFolderListFullItemViewModel(f))
          .OrderBy(pf => pf.Status)
          .ThenBy(pf => pf.PlotFolderMasterTitle);
    }
  }

  public class PlotFolderListFullItemViewModel : PlotFolderListItemViewModel
  {
    public MarkdownViewModel Summary { get; }
    public IEnumerable<PlotElementViewModel> Elements { get; }
    public string ElementTodos { get; }

    public PlotFolderListFullItemViewModel(PlotFolder folder) : base(folder)
    {
      Summary = new MarkdownViewModel(folder.MasterSummary);
      Elements = folder.Elements.ToViewModels(hasMasterAccess: true);
      ElementTodos = folder.Elements.Select(e => e.Texts.TodoField).WhereNotNullOrWhiteSpace().JoinStrings("\n");
    }
  }

  public class PlotFolderListItemViewModel : PlotFolderViewModelBase
  {
    public int PlotFolderId { get; }
    public int ElementsCount { get; }

    public PlotFolderListItemViewModel(PlotFolder folder)
    {
      PlotFolderId = folder.PlotFolderId;
      PlotFolderMasterTitle = folder.MasterTitle;
      ProjectId = folder.ProjectId;
      Status = GetStatus(folder);
      ElementsCount = folder.Elements.Count;
      TodoField = folder.TodoField;
    }
  }
}
