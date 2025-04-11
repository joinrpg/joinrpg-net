using System.ComponentModel;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Plots;

namespace JoinRpg.Web.Models.Plot;

public abstract class PlotFolderListViewModelBase(ProjectInfo project, ICurrentUserAccessor currentUser)
{
    public bool HasEditAccess { get; } = project.HasMasterAccess(currentUser, Permission.CanManagePlots);

    public int ProjectId { get; } = project.ProjectId;
    public string ProjectName { get; } = project.ProjectName;

}

[ReadOnly(true)]
public class PlotFolderListViewModelForGroup(IEnumerable<PlotFolder> folders, ICurrentUserAccessor currentUser, CharacterGroupDetailsViewModel groupNavigation, ProjectInfo projectInfo) : PlotFolderListViewModel(folders, currentUser, projectInfo)
{
    public CharacterGroupDetailsViewModel GroupNavigation { get; } = groupNavigation;
}

[ReadOnly(true)]
public class PlotFolderListViewModel : PlotFolderListViewModelBase, IPlotFolderListViewModel
{
    public IEnumerable<PlotFolderListItemViewModel> Folders { get; }
    public bool HasMasterAccess { get; private set; }

    public IReadOnlyCollection<PlotFolderIdentification> FolderIds { get; private set; }

    public PlotFolderListViewModel(IEnumerable<PlotFolder> folders, ICurrentUserAccessor currentUser, ProjectInfo projectInfo)
      : base(projectInfo, currentUser)
    {
        HasMasterAccess = projectInfo.HasMasterAccess(currentUser);
        Folders =
          folders
            .Select(f => new PlotFolderListItemViewModel(f, currentUser));

        FolderIds = [.. Folders.Select(f => f.PlotFolderId)];
    }
}

public class PlotFolderFullListViewModel : PlotFolderListViewModelBase
{
    public IEnumerable<PlotFolderListFullItemViewModel> Folders { get; }
    public bool InWorkOnly { get; }

    public PlotFolderFullListViewModel(IEnumerable<PlotFolder> folders, ICurrentUserAccessor currentUser, IUriService uriService, ProjectInfo projectInfo, bool inWorkOnly = false)
      : base(projectInfo, currentUser)
    {
        InWorkOnly = inWorkOnly;
        Folders =
          folders
            .Select(f => new PlotFolderListFullItemViewModel(f, currentUser, uriService, projectInfo))
            .OrderBy(pf => pf.Status)
            .ThenBy(pf => pf.PlotFolderMasterTitle);
    }
}

public class PlotFolderListFullItemViewModel : PlotFolderListItemViewModel
{
    public JoinHtmlString Summary { get; }
    public IEnumerable<PlotElementViewModel> Elements { get; }
    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(TodoField) || Elements.Any(e => e.HasWorkTodo);

    public PlotFolderListFullItemViewModel(PlotFolder folder, ICurrentUserAccessor currentUser, IUriService uriService, ProjectInfo projectInfo) : base(folder, currentUser)
    {
        Summary = folder.MasterSummary.ToHtmlString();

        if (folder.Elements.Count == 0)
        {
            Elements = [];
            return;
        }

        var linkRenderer = new JoinrpgMarkdownLinkRenderer(folder.Elements.First().Project, projectInfo);

        Elements = folder.Elements.Where(p => p.ElementType == PlotElementType.RegularPlot)
          .Select(
            p => new PlotElementViewModel(null, currentUser, linkRenderer, p.LastVersion(), uriService, projectInfo))
          .MarkFirstAndLast();
    }
}

public class PlotFolderListItemViewModel : PlotFolderViewModelBase, IPlotFolderListItemViewModel
{
    public PlotFolderIdentification PlotFolderId { get; }
    public int ElementsCount { get; }
    public bool HasEditAccess { get; }

    public IEnumerable<string> TagNames { get; }

    public PlotFolderListItemViewModel(PlotFolder folder, ICurrentUserAccessor currentUser)
    {
        PlotFolderId = new(folder.ProjectId, folder.PlotFolderId);
        PlotFolderMasterTitle = folder.MasterTitle;
        TagNames = folder.PlotTags.Select(tag => tag.TagName).OrderBy(tag => tag).ToList();
        ProjectId = folder.ProjectId;
        Status = folder.GetStatus();
        ElementsCount = folder.Elements.Count;
        TodoField = folder.TodoField;
        HasEditAccess = folder.HasMasterAccess(currentUser, Permission.CanManagePlots) && folder.Project.Active;
    }
}
