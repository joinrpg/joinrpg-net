namespace JoinRpg.Web.Plots;

public interface IPlotFolderListViewModel
{
    bool HasMasterAccess { get; }

    bool HasEditAccess { get; }

    bool HasPlotEditorAccess { get; }

    IReadOnlyCollection<PlotFolderIdentification> FolderIds { get; }
}
