using JoinRpg.PrimitiveTypes.Access;

namespace JoinRpg.Web.Plots;

public record class PlotFolderListItemViewModel(
    PlotFolderIdentification PlotFolderId,
    string Name,
    int ElementsCount,
    string TodoField,
    IReadOnlyCollection<string> TagNames,
    PlotStatus Status,
    PlotAccessArguments PlotAccessArguments
    ) : IPlotFolderListItemViewModel, IPlotFolderLink
{
    public bool HasEditAccess => PlotAccessArguments.HasEditAccess;

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

    public IReadOnlyCollection<PlotFolderIdentification> FolderIds { get; } = Folders.Select(f => f.PlotFolderId).ToList();
}
