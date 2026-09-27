namespace JoinRpg.Web.Plots.Folders;

public record PlotFolderDto(PlotFolderIdentification PlotFolderId, string Name) : IPlotFolderLink;

public interface IPlotFolderLink
{
    PlotFolderIdentification PlotFolderId { get; }
    string Name { get; }
}
