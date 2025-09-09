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

public record class PlotFolderListViewModelForGroup(PlotFolderListViewModel FolderViewModel, CharacterGroupDetailsViewModel GroupNavigation);

public class PlotFolderFullListViewModel
{
    public IEnumerable<PlotFolderListFullItemViewModel> Folders { get; }
    public bool InWorkOnly { get; }

    public string ProjectName { get; }

    public PlotFolderFullListViewModel(IReadOnlyCollection<PlotFolder> folders, ICurrentUserAccessor currentUser, ProjectInfo projectInfo, bool inWorkOnly = false)
    {
        ProjectName = projectInfo.ProjectName;
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

public class PlotFolderListFullItemViewModel : PlotFolderViewModelBase, IPlotFolderListItemViewModel
{
    public JoinHtmlString Summary { get; }
    public IReadOnlyCollection<PlotElementListItemViewModel> Elements { get; }

    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(TodoField) || Elements.Any(e => e.Status != PlotStatus.Completed);

    public PlotFolderIdentification PlotFolderId { get; }
    public int ElementsCount { get; }
    public bool HasEditAccess { get; }

    public IEnumerable<string> TagNames { get; }

    public PlotFolderListFullItemViewModel(PlotFolder folder, ICurrentUserAccessor currentUser, ProjectInfo projectInfo, JoinrpgMarkdownLinkRenderer linkRenderer)
    {
        PlotFolderId = new(folder.ProjectId, folder.PlotFolderId);
        PlotFolderMasterTitle = folder.MasterTitle;
        TagNames = [.. folder.PlotTags.Select(tag => tag.TagName).Order()];
        ProjectId = folder.ProjectId;
        Status = folder.GetStatus();
        ElementsCount = folder.Elements.Count(x => x.IsActive);
        TodoField = folder.TodoField;
        HasEditAccess = folder.HasMasterAccess(currentUser, Permission.CanManagePlots) && folder.Project.Active;
        Elements = PlotElementListItemViewModel.FromFolder(folder, currentUser, projectInfo, linkRenderer);
        Summary = folder.MasterSummary.ToHtmlString();
    }
}
