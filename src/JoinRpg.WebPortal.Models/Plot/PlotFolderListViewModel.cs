using JoinRpg.Data.Interfaces.Plots;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Plots;
using JoinRpg.Web.Plots.Folders;

namespace JoinRpg.Web.Models.Plot;

public record class PlotFolderListViewModelForGroup(PlotFolderListViewModel FolderViewModel, CharacterGroupDetailsViewModel GroupNavigation);

public class PlotFolderFullListViewModel
{
    public IEnumerable<PlotFolderListFullItemViewModel> Folders { get; }
    public bool InWorkOnly { get; }

    public string ProjectName { get; }

    public PlotFolderFullListViewModel(IReadOnlyCollection<PlotFolderDetailsDto> folders, ILinkRenderer linkRenderer, ICurrentUserAccessor currentUser, ProjectInfo projectInfo, bool inWorkOnly = false)
    {
        ProjectName = projectInfo.ProjectName;
        InWorkOnly = inWorkOnly;

        if (folders.Count == 0)
        {
            Folders = [];
        }
        else
        {
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

    public PlotFolderListFullItemViewModel(PlotFolderDetailsDto folder, ICurrentUserAccessor currentUser, ProjectInfo projectInfo, ILinkRenderer linkRenderer)
    {
        PlotFolderId = folder.Id;
        PlotFolderMasterTitle = folder.MasterTitle;
        TagNames = folder.Tags;
        ProjectId = folder.Id.ProjectId.Value;
        Status = folder.GetStatus();
        ElementsCount = folder.Elements.Count(x => x.IsActive);
        TodoField = folder.TodoField;
        HasEditAccess = projectInfo.HasMasterAccess(currentUser, Permission.CanManagePlots) && projectInfo.IsActive;
        Elements = PlotElementListItemViewModel.FromFolder(folder, currentUser, projectInfo, linkRenderer);
        Summary = folder.MasterSummary.ToHtmlString();
    }
}
