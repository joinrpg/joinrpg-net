namespace JoinRpg.Web.Plots.Folders;

public interface IPlotFolderListItemViewModel
{
    PlotStatus Status { get; }

    PlotFolderIdentification PlotFolderId { get; }
}
