using System.ComponentModel;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Plots;

namespace JoinRpg.Web.Models.Plot;

public abstract class PlotFolderListViewModelBase(ProjectInfo project, ICurrentUserAccessor currentUser)
{
    public bool HasEditAccess { get; } = project.HasMasterAccess(currentUser, Permission.CanManagePlots) && project.ProjectStatus != ProjectLifecycleStatus.Archived;

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

    public PlotFolderFullListViewModel(IReadOnlyCollection<PlotFolder> folders, ICurrentUserAccessor currentUser, ProjectInfo projectInfo, bool inWorkOnly = false) : base(projectInfo, currentUser)
    {
        InWorkOnly = inWorkOnly;

        if (folders.Count == 0)
        {
            Folders = [];
        }
        else
        {
            var linkRenderer = new JoinrpgMarkdownLinkRenderer(folders.First().Project, projectInfo);

            //TODO правильная сортировка
            Folders =
              folders
                .Select(f => new PlotFolderListFullItemViewModel(f, currentUser, projectInfo, linkRenderer))
                .Where(f => !InWorkOnly || f.HasWorkTodo)
                .OrderBy(pf => pf.Status)
                .ThenBy(pf => pf.PlotFolderMasterTitle);

        }
    }
}

public class PlotFolderListFullItemViewModel
    (PlotFolder folder, ICurrentUserAccessor currentUser, ProjectInfo projectInfo, JoinrpgMarkdownLinkRenderer linkRenderer)
    : PlotFolderListItemViewModel(folder, currentUser)
{
    public JoinHtmlString Summary { get; } = folder.MasterSummary.ToHtmlString();
    public IReadOnlyCollection<PlotRenderedTextViewModel> Elements { get; } = [.. folder.Elements
            .Where(p => p.IsActive)
            .Where(p => p.ElementType == PlotElementType.RegularPlot)
            .Select(p => p.GetDtoForLast().Render(linkRenderer, projectInfo, currentUser))];
    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(TodoField) || Elements.Any(e => e.HasWorkTodo);
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
        TagNames = [.. folder.PlotTags.Select(tag => tag.TagName).Order()];
        ProjectId = folder.ProjectId;
        Status = folder.GetStatus();
        ElementsCount = folder.Elements.Count(x => x.IsActive);
        TodoField = folder.TodoField;
        HasEditAccess = folder.HasMasterAccess(currentUser, Permission.CanManagePlots) && folder.Project.Active;
    }
}
