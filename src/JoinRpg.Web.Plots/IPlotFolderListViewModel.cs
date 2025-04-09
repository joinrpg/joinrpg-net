namespace JoinRpg.Web.Plots;

public interface IPlotFolderListViewModel
{
    bool HasMasterAccess { get; }

    bool HasEditAccess { get; }

    IReadOnlyCollection<PlotFolderIdentification> FolderIds { get; }
}
