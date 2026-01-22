namespace JoinRpg.Web.Plots;

public interface IPlotFolderListItemViewModel
{
    PlotStatus Status { get; }

    PlotFolderIdentification PlotFolderId { get; }
}
