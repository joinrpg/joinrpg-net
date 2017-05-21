using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.CommonTypes;

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
  public class PlotFolderListViewModelForGroup : PlotFolderListViewModel
  {
    public CharacterGroupDetailsViewModel GroupNavigation { get; }

    public PlotFolderListViewModelForGroup(IEnumerable<PlotFolder> folders, Project project, int? currentUserId, CharacterGroupDetailsViewModel groupNavigation)
      : base(folders, project, currentUserId)
    {
      GroupNavigation = groupNavigation;
    }
  }

  [ReadOnly(true)]
  public class PlotFolderListViewModel : PlotFolderListViewModelBase
  {
    public IEnumerable<PlotFolderListItemViewModel> Folders { get; }
    public bool HasMasterAccess { get; private set; }

    public PlotFolderListViewModel(IEnumerable<PlotFolder> folders, Project project, int? currentUserId)
      : base(project, project.HasMasterAccess(currentUserId, acl => acl.CanManagePlots))
    {
      HasMasterAccess = project.HasMasterAccess(currentUserId);
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

      if (folder.Elements.Any())
      {

        var linkRenderer = new JoinrpgMarkdownLinkRenderer(folder.Elements.First().Project);

        Elements = folder.Elements.Where(p => p.ElementType == PlotElementType.RegularPlot)
          .Select(
            p => new PlotElementViewModel(null, currentUserId, linkRenderer, p.LastVersion()))
          .MarkFirstAndLast();
      }
      else
      {
        Elements = Enumerable.Empty<PlotElementViewModel>();
      }
    }
  }

  public class PlotFolderListItemViewModel : PlotFolderViewModelBase
  {
    public int PlotFolderId { get; }
    public int ElementsCount { get; }
    public bool HasEditAccess { get; }

    public IEnumerable<string> TagNames { get; }

    public PlotFolderListItemViewModel(PlotFolder folder, int? currentUserId)
    {
      PlotFolderId = folder.PlotFolderId;
      PlotFolderMasterTitle = folder.MasterTitle;
      TagNames = folder.PlotTags.Select(tag => tag.TagName).OrderBy(tag => tag).ToList();
      ProjectId = folder.ProjectId;
      Status = folder.GetStatus();
      ElementsCount = folder.Elements.Count;
      TodoField = folder.TodoField;
      HasEditAccess = folder.HasMasterAccess(currentUserId, acl => acl.CanManagePlots) && folder.Project.Active;
    }
  }
}
