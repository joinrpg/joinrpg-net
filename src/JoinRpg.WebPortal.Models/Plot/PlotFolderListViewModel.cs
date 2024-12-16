using System.ComponentModel;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Plot;

public abstract class PlotFolderListViewModelBase(ProjectInfo project, bool hasEditAccess)
{
    public bool HasEditAccess { get; } = hasEditAccess;

    public int ProjectId { get; } = project.ProjectId;
    public string ProjectName { get; } = project.ProjectName;

}

[ReadOnly(true)]
public class PlotFolderListViewModelForGroup(IEnumerable<PlotFolder> folders, Project project, int? currentUserId, CharacterGroupDetailsViewModel groupNavigation, ProjectInfo projectInfo) : PlotFolderListViewModel(folders, project, currentUserId, projectInfo)
{
    public CharacterGroupDetailsViewModel GroupNavigation { get; } = groupNavigation;
}

[ReadOnly(true)]
public class PlotFolderListViewModel : PlotFolderListViewModelBase
{
    public IEnumerable<PlotFolderListItemViewModel> Folders { get; }
    public bool HasMasterAccess { get; private set; }

    public PlotFolderListViewModel(IEnumerable<PlotFolder> folders, Project project, int? currentUserId, ProjectInfo projectInfo)
      : base(projectInfo, project.HasMasterAccess(currentUserId, acl => acl.CanManagePlots))
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

    public PlotFolderFullListViewModel(IEnumerable<PlotFolder> folders, Project project, int? currentUserId, IUriService uriService, ProjectInfo projectInfo, bool inWorkOnly = false)
      : base(projectInfo, project.HasMasterAccess(currentUserId, acl => acl.CanManagePlots))
    {
        InWorkOnly = inWorkOnly;
        Folders =
          folders
            .Select(f => new PlotFolderListFullItemViewModel(f, currentUserId, uriService, projectInfo))
            .OrderBy(pf => pf.Status)
            .ThenBy(pf => pf.PlotFolderMasterTitle);
    }
}

public class PlotFolderListFullItemViewModel : PlotFolderListItemViewModel
{
    public JoinHtmlString Summary { get; }
    public IEnumerable<PlotElementViewModel> Elements { get; }
    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(TodoField) || Elements.Any(e => e.HasWorkTodo);

    public PlotFolderListFullItemViewModel(PlotFolder folder, int? currentUserId, IUriService uriService, ProjectInfo projectInfo) : base(folder, currentUserId)
    {
        Summary = folder.MasterSummary.ToHtmlString();

        if (folder.Elements.Any())
        {

            var linkRenderer = new JoinrpgMarkdownLinkRenderer(folder.Elements.First().Project, projectInfo);

            Elements = folder.Elements.Where(p => p.ElementType == PlotElementType.RegularPlot)
              .Select(
                p => new PlotElementViewModel(null, currentUserId, linkRenderer, p.LastVersion(), uriService))
              .MarkFirstAndLast();
        }
        else
        {
            Elements = [];
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
