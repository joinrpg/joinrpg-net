namespace JoinRpg.Web.Plots.Folders;

public record class PlotFolderListItemViewModel(
    PlotFolderIdentification PlotFolderId,
    string Name,
    int ElementsCount,
    string TodoField,
    IReadOnlyCollection<string> TagNames,
    PlotStatus Status,
    PlotAccessArguments PlotAccessArguments
    ) : IPlotFolderListItemViewModel, IPlotFolderLink, IMoveableListItem
{
    public bool HasEditAccess => PlotAccessArguments.HasEditAccess;

    /// <summary>Перестановка сюжетов в списке без перезагрузки (#238): родитель — проект.</summary>
    string IMoveableListItem.Id => PlotFolderId.ToString();

    string IMoveableListItem.ParentId => PlotFolderId.ProjectId.ToString();

    string IMoveableListItem.DisplayText => Name;

    string IMoveableListItem.Subtext => TodoField;
}

public record class PlotFolderListViewModel(
    ProjectIdentification ProjectId,
    IReadOnlyCollection<PlotFolderListItemViewModel> Folders,
    PlotAccessArguments PlotAccessArguments,
    string Title
    ) : IPlotFolderListViewModel
{
    public bool HasMasterAccess => PlotAccessArguments.HasMasterAccess;

    public bool HasEditAccess => PlotAccessArguments.HasEditAccess;

    public bool HasPlotEditorAccess => PlotAccessArguments.HasPlotEditorAccess;
}
