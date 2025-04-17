using System.ComponentModel;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes;
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

public class PlotFolderFullListViewModel(IEnumerable<PlotFolder> folders, ICurrentUserAccessor currentUser, ProjectInfo projectInfo, bool inWorkOnly = false) : PlotFolderListViewModelBase(projectInfo, currentUser)
{
    //TODO правильная сортировка
    public IEnumerable<PlotFolderListFullItemViewModel> Folders { get; } =
          folders
            .Select(f => new PlotFolderListFullItemViewModel(f, currentUser, projectInfo))
            .OrderBy(pf => pf.Status)
            .ThenBy(pf => pf.PlotFolderMasterTitle);
    public bool InWorkOnly { get; } = inWorkOnly;

    public bool ShowEditorControls { get; } = projectInfo.HasMasterAccess(currentUser) && projectInfo.ProjectStatus != ProjectLifecycleStatus.Archived;
}

public class PlotFolderListFullItemViewModel : PlotFolderListItemViewModel
{
    public JoinHtmlString Summary { get; }
    public IEnumerable<PlotElementViewModel> Elements { get; }
    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(TodoField) || Elements.Any(e => e.HasWorkTodo);

    public PlotFolderListFullItemViewModel(PlotFolder folder, ICurrentUserAccessor currentUser, ProjectInfo projectInfo) : base(folder, currentUser)
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
            p => new PlotElementViewModel(null, p.GetDtoForLast().Render(linkRenderer, projectInfo, currentUser), projectInfo.HasMasterAccess(currentUser) && projectInfo.ProjectStatus != ProjectLifecycleStatus.Archived));
    }
}

public static class PlotTextDtoBuilder
{
    public static PlotTextDto GetDtoForLast(this PlotElement element)
    {
        var version = element.LastVersion();
        return new PlotTextDto()
        {
            Completed = element.GetStatus() == PlotStatus.Completed,
            HasPublished = element.Published != null,
            Latest = true,
            Published = element.Published == version.Version,
            Content = version.Content,
            TodoField = version.TodoField,
            Id = new PlotVersionIdentification(element.ProjectId, element.PlotFolderId, element.PlotElementId, version.Version),
            IsActive = element.IsActive,
            Target = new TargetsInfo(
                    [.. element.TargetCharacters.Select(x => new CharacterTarget(x.GetId(), x.CharacterName))],
                    [.. element.TargetGroups.Select(x => new GroupTarget(x.GetId(), x.CharacterGroupName))])
        };
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
