using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.Plot
{
  public abstract class PlotFolderListViewModelBase
  {
    public bool HasEditAccess { get; }

    protected PlotFolderListViewModelBase (Project project, bool hasEditAccess)
    {
      HasEditAccess = hasEditAccess;

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

    public PlotFolderListViewModel(IEnumerable<PlotFolder> folders, Project project, int? currentUserId)
      : base(project, project.HasMasterAccess(currentUserId, acl => acl.CanManagePlots))
    {
      Folders =
        folders
          .Select(f => new PlotFolderListItemViewModel(f, currentUserId))
          .OrderBy(pf => pf.Status)
          .ThenBy(pf => pf.PlotFolderMasterTitle);
    }
  }

  public class PlotFolderFullListViewModel : PlotFolderListViewModelBase
  {
    public IEnumerable<PlotFolderListFullItemViewModel> Folders { get; }
    public bool InWorkOnly { get; }

    public PlotFolderFullListViewModel(IEnumerable<PlotFolder> folders, Project project,
      int? currentUserId, bool inWorkOnly = false)
      : base(project, project.HasMasterAccess(currentUserId, acl => acl.CanManagePlots))
    {
      InWorkOnly = inWorkOnly;
      Folders =
        folders
          .Select(f => new PlotFolderListFullItemViewModel(f, currentUserId))
          .OrderBy(pf => pf.Status)
          .ThenBy(pf => pf.PlotFolderMasterTitle);
    }
  }

  public class PlotFolderListFullItemViewModel : PlotFolderListItemViewModel
  {
    public IHtmlString Summary { get; }
    public IEnumerable<PlotElementViewModel> Elements { get; }
    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(TodoField) || Elements.Any(e => e.HasWorkTodo);

    public PlotFolderListFullItemViewModel(PlotFolder folder, int? currentUserId) : base(folder, currentUserId)
    {
      Summary = folder.MasterSummary.ToHtmlString();
      Elements = folder.Elements.ToViewModels(currentUserId);
    }
  }

  public class PlotFolderListItemViewModel : PlotFolderViewModelBase
  {
    public int PlotFolderId { get; }
    public int ElementsCount { get; }
    public bool HasEditAccess { get; }

    public PlotFolderListItemViewModel(PlotFolder folder, int? currentUserId)
    {
      PlotFolderId = folder.PlotFolderId;
      PlotFolderMasterTitle = folder.MasterTitle;
      ProjectId = folder.ProjectId;
      Status = folder.GetStatus();
      ElementsCount = folder.Elements.Count;
      TodoField = folder.TodoField;
      HasEditAccess = folder.HasMasterAccess(currentUserId, acl => acl.CanManagePlots) && folder.Project.Active;
    }
  }
}
